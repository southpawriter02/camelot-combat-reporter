import unittest
import os
from datetime import datetime
from src.log_parser import LogParser
from src.models import DamageEvent

class TestLogParser(unittest.TestCase):

    def test_parse_damage_dealt(self):
        # This test will fail initially
        log_content = "[01:23:45] You hit a goblin for 25 points of slash damage!"
        log_file_path = "test_log.log"
        with open(log_file_path, "w") as f:
            f.write(log_content)

        parser = LogParser(log_file_path)
        events = list(parser.parse())

        self.assertEqual(len(events), 1)
        event = events[0]
        self.assertIsInstance(event, DamageEvent)
        self.assertEqual(event.timestamp, datetime.strptime("01:23:45", "%H:%M:%S").time())
        self.assertEqual(event.source, "You")
        self.assertEqual(event.target, "a goblin")
        self.assertEqual(event.damage_amount, 25)
        self.assertEqual(event.damage_type, "slash")

        os.remove(log_file_path)

if __name__ == '__main__':
    unittest.main()
