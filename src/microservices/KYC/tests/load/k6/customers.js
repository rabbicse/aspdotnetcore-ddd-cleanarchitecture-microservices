// k6 load test for the KYC API.
//
//   docker compose up --build -d
//   docker compose --profile loadtest run --rm k6                      # smoke (default)
//   K6_SCENARIO=load   docker compose --profile loadtest run --rm k6   # sustained, realistic load
//   K6_SCENARIO=stress docker compose --profile loadtest run --rm k6   # find the breaking point
//
// Or with a local k6 install:
//   k6 run -e BASE_URL=http://localhost:8080 -e SCENARIO=load tests/load/k6/customers.js
//
// What one iteration does (a realistic read-heavy mix):
//   1. POST /api/customers            register a customer          (write path: SQL Server -> Mongo -> Redis -> Kafka)
//   2. GET  /api/customers/{id}       read it back (3 times)       (read path: Redis cache -> Mongo)
//   3. GET  /api/customers            list, on ~2% of iterations   (unpaged: it returns ALL customers, so keep it rare)

import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { Counter } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const SCENARIO = __ENV.SCENARIO || 'smoke';

const customersCreated = new Counter('customers_created');

const scenarios = {
  // Does it work at all? 1 user for 30 seconds. Run this on every build.
  smoke: {
    executor: 'constant-vus',
    vus: 1,
    duration: '30s',
  },
  // Typical sustained traffic: ramp to 20 users, hold, ramp down.
  load: {
    executor: 'ramping-vus',
    startVUs: 0,
    stages: [
      { duration: '1m', target: 20 },
      { duration: '3m', target: 20 },
      { duration: '30s', target: 0 },
    ],
    gracefulRampDown: '15s',
  },
  // Beyond expected traffic: keep climbing until something gives.
  stress: {
    executor: 'ramping-vus',
    startVUs: 0,
    stages: [
      { duration: '1m', target: 50 },
      { duration: '2m', target: 100 },
      { duration: '2m', target: 200 },
      { duration: '1m', target: 0 },
    ],
    gracefulRampDown: '30s',
  },
  // A sudden burst of traffic, then recovery.
  spike: {
    executor: 'ramping-vus',
    startVUs: 0,
    stages: [
      { duration: '30s', target: 10 },
      { duration: '10s', target: 150 },
      { duration: '1m', target: 150 },
      { duration: '10s', target: 10 },
      { duration: '1m', target: 10 },
    ],
  },
};

if (!scenarios[SCENARIO]) {
  throw new Error(`Unknown SCENARIO "${SCENARIO}". Use one of: ${Object.keys(scenarios).join(', ')}`);
}

export const options = {
  scenarios: { [SCENARIO]: scenarios[SCENARIO] },
  thresholds: {
    // The run FAILS (non-zero exit) if any of these are not met.
    http_req_failed: ['rate<0.01'],                       // under 1% errors
    'http_req_duration{name:create}': ['p(95)<800'],      // writes: p95 under 800 ms
    'http_req_duration{name:get_by_id}': ['p(95)<300'],   // cached reads: p95 under 300 ms
    'http_req_duration{name:list}': ['p(95)<1500'],       // unpaged list: p95 under 1.5 s
    checks: ['rate>0.99'],
  },
};

const JSON_HEADERS = { headers: { 'Content-Type': 'application/json' } };

function newCustomer() {
  const unique = `${__VU}-${__ITER}-${Date.now()}`;
  return JSON.stringify({
    firstName: 'Load',
    lastName: `Test-${unique}`,
    dob: '1990-05-17',
    customerAddress: {
      street: '12 St James Square',
      city: 'London',
      country: 'United Kingdom',
      zipCode: 'SW1Y 4JH',
    },
  });
}

export function setup() {
  // Fail fast with a clear message instead of thousands of connection errors.
  const ready = http.get(`${BASE_URL}/health/ready`);
  if (ready.status !== 200) {
    throw new Error(`The API at ${BASE_URL} is not ready (GET /health/ready returned ${ready.status}).`);
  }
}

export default function () {
  let customerId;

  group('register', () => {
    const response = http.post(`${BASE_URL}/api/customers`, newCustomer(), {
      ...JSON_HEADERS,
      tags: { name: 'create' },
    });

    const created = check(response, {
      'create: status is 201': (r) => r.status === 201,
      'create: returns an id': (r) => r.status === 201 && !!r.json('id'),
    });

    if (created) {
      customersCreated.add(1);
      customerId = response.json('id');
    }
  });

  if (customerId) {
    group('read back', () => {
      for (let i = 0; i < 3; i++) {
        const response = http.get(`${BASE_URL}/api/customers/${customerId}`, {
          tags: { name: 'get_by_id' },
        });
        check(response, {
          'get: status is 200': (r) => r.status === 200,
          'get: returns the same id': (r) => r.status === 200 && r.json('id') === customerId,
        });
        sleep(0.2);
      }
    });
  }

  if (Math.random() < 0.02) {
    group('list', () => {
      const response = http.get(`${BASE_URL}/api/customers`, { tags: { name: 'list' } });
      check(response, { 'list: status is 200': (r) => r.status === 200 });
    });
  }

  sleep(1);
}
