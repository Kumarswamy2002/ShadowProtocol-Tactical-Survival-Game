"""
Dynamic Environmental Weather & Hazard System
"""
from typing import Dict, Any

class WeatherSystem:
    WEATHER_EFFECTS = {
        "CLEAR": {"visibility_mod": 1.0, "stamina_drain_mod": 1.0},
        "HEAVY_FOG": {"visibility_mod": 0.4, "stamina_drain_mod": 1.1},
        "RADIATION_STORM": {"visibility_mod": 0.6, "stamina_drain_mod": 1.5, "hp_decay_per_sec": 2.0},
        "ACID_RAIN": {"visibility_mod": 0.7, "stamina_drain_mod": 1.2, "armor_degrade_rate": 1.5}
    }

    @classmethod
    def get_weather_modifiers(cls, weather_type: str) -> Dict[str, Any]:
        return cls.WEATHER_EFFECTS.get(weather_type, cls.WEATHER_EFFECTS["CLEAR"])
