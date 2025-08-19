from dataclasses import dataclass
from datetime import datetime

@dataclass
class LogEvent:
    """Base class for all log events."""
    timestamp: datetime

@dataclass
class DamageEvent(LogEvent):
    """Represents a damage event."""
    source: str
    target: str
    damage_amount: int
    damage_type: str

@dataclass
class HealingEvent(LogEvent):
    """Represents a healing event."""
    source: str
    target: str
    healing_amount: int
