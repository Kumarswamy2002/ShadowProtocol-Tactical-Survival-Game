from Backend.src.systems.weather_system import WeatherSystem

def test_weather_modifiers():
    fog = WeatherSystem.get_weather_modifiers("HEAVY_FOG")
    assert fog["visibility_mod"] == 0.4
    clear = WeatherSystem.get_weather_modifiers("UNKNOWN_TYPE")
    assert clear["visibility_mod"] == 1.0
