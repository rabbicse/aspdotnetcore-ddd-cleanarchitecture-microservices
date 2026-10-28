#!/usr/bin/env python3
"""Turn failed tests (TRX files) and optional log tails into GitHub annotations.

Annotations show on the PR and in the Checks API, so failures can be read without
downloading artifacts. Usage: report-failures.py <results-dir> [--log FILE ...]
"""
import glob
import os
import sys
import xml.etree.ElementTree as ET

NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"


def escape(text: str) -> str:
    return text.replace("%", "%25").replace("\r", "").replace("\n", "%0A")


def report_trx(results_dir: str) -> int:
    failures = 0
    for path in sorted(glob.glob(os.path.join(results_dir, "**", "*.trx"), recursive=True)):
        root = ET.parse(path).getroot()
        for result in root.iter(f"{NS}UnitTestResult"):
            if result.get("outcome") != "Failed":
                continue
            failures += 1
            message = result.findtext(f"{NS}Output/{NS}ErrorInfo/{NS}Message") or ""
            stack = result.findtext(f"{NS}Output/{NS}ErrorInfo/{NS}StackTrace") or ""
            stack = "\n".join(stack.strip().splitlines()[:8])
            body = f"{message.strip()}\n{stack}"[:3500]
            print(f"::error title=Test failed: {result.get('testName')}::{escape(body)}")
    return failures


def report_logs(paths: list[str]) -> None:
    for path in paths:
        if not os.path.exists(path):
            continue
        with open(path, encoding="utf-8", errors="replace") as handle:
            lines = handle.read().splitlines()
        interesting = [line for line in lines if any(word in line for word in ("fail", "Fail", "error", "Error", "Exception", "exception"))]
        tail = (interesting or lines)[-40:]
        print(f"::error title=Log excerpt: {os.path.basename(path)}::{escape(chr(10).join(tail)[-3500:])}")


if __name__ == "__main__":
    args = sys.argv[1:]
    logs = []
    if "--log" in args:
        index = args.index("--log")
        logs = args[index + 1:]
        args = args[:index]
    count = report_trx(args[0] if args else "TestResults")
    report_logs(logs)
    print(f"{count} failed test(s) reported.")
