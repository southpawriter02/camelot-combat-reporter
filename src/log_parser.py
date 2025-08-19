import re
from datetime import datetime
from .models import DamageEvent

class LogParser:
    """
    Parses a DAoC log file and extracts combat data.
    """
    def __init__(self, log_file_path: str):
        """
        Initializes the LogParser with the path to the log file.

        :param log_file_path: The path to the chat.log or combat.log file.
        """
        self.log_file_path = log_file_path
        self.damage_dealt_pattern = re.compile(
            r'\[(?P<timestamp>\d{2}:\d{2}:\d{2})\]\s+You hit (the )?(?P<target>.+?) for (?P<amount>\d+) points of((?P<type> \w+))? damage[!.]?'
        )

    def parse(self):
        """
        Parses the log file and yields structured data for each relevant log entry.
        """
        try:
            with open(self.log_file_path, 'r', encoding='utf-8') as f:
                for line in f:
                    match = self.damage_dealt_pattern.match(line)
                    if match:
                        data = match.groupdict()
                        damage_type = data.get('type')
                        if damage_type:
                            damage_type = damage_type.strip()
                        else:
                            damage_type = 'Unknown'

                        yield DamageEvent(
                            timestamp=datetime.strptime(data['timestamp'], '%H:%M:%S').time(),
                            source='You',
                            target=data['target'].strip(),
                            damage_amount=int(data['amount']),
                            damage_type=damage_type
                        )
        except FileNotFoundError:
            print(f"Error: Log file not found at {self.log_file_path}")
            return
        except Exception as e:
            print(f"An error occurred: {e}")
            return
