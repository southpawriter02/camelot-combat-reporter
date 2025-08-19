import argparse
from src.log_parser import LogParser

def main():
    """
    The main entry point for the DAoC Log Parser application.
    """
    parser = argparse.ArgumentParser(description="Parse a DAoC log file.")
    parser.add_argument("log_file", help="The path to the log file to parse.")
    args = parser.parse_args()

    log_parser = LogParser(args.log_file)
    for event in log_parser.parse():
        print(event)

if __name__ == "__main__":
    main()
