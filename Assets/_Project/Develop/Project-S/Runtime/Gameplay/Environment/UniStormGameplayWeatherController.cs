using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniStorm;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Environment
{
    public sealed class UniStormGameplayWeatherController : MonoBehaviour
    {
        private const int StartHour = 13;
        private const int StartMinute = 0;
        private const int DayLengthMinutes = 45;
        private const int NightLengthMinutes = 20;
        private const float MinWeatherIntervalSeconds = 12f * 60f;
        private const float MaxWeatherIntervalSeconds = 15f * 60f;
        private const float WeatherSoundsVolume = 0.35f;
        private const float AmbienceVolume = 0.55f;

        private static UniStormGameplayWeatherController _instance;

        private readonly string[] _weatherRotation =
        {
            "Partly Cloudy",
            "Mostly Clear",
            "Mostly Cloudy",
            "Cloudy",
            "Hazy",
            "Light Rain",
            "Overcast",
            "Clear"
        };

        private UniStormSystem _uniStorm;
        private List<WeatherType> _availableWeather;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            if (_instance != null)
                return;

            var gameObject = new GameObject("[UniStorm Gameplay Weather Controller]");
            _instance = gameObject.AddComponent<UniStormGameplayWeatherController>();
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            StartCoroutine(SetupWhenUniStormIsAvailable());
        }

        private IEnumerator SetupWhenUniStormIsAvailable()
        {
            while (true)
            {
                yield return new WaitUntil(() => FindObjectOfType<UniStormSystem>() != null);

                _uniStorm = FindObjectOfType<UniStormSystem>();
                ConfigureTimeAndAudio(_uniStorm);

                yield return new WaitUntil(() => _uniStorm == null || _uniStorm.UniStormInitialized);

                if (_uniStorm != null)
                {
                    ConfigureWeather(_uniStorm);
                    yield return RunWeatherLoop(_uniStorm);
                }
            }
        }

        private void ConfigureTimeAndAudio(UniStormSystem uniStorm)
        {
            uniStorm.RealWorldTime = UniStormSystem.EnableFeature.Disabled;
            uniStorm.TimeFlow = UniStormSystem.EnableFeature.Enabled;
            uniStorm.Hour = StartHour;
            uniStorm.Minute = StartMinute;
            uniStorm.StartingHour = StartHour;
            uniStorm.StartingMinute = StartMinute;
            uniStorm.m_TimeFloat = StartHour / 24f + StartMinute / 1440f;

            uniStorm.DayLength = DayLengthMinutes;
            uniStorm.NightLength = NightLengthMinutes;

            uniStorm.WeatherSoundsVolume = WeatherSoundsVolume;
            uniStorm.AmbienceVolume = AmbienceVolume;
            uniStorm.TimeOfDaySoundsSecondsMin = 35;
            uniStorm.TimeOfDaySoundsSecondsMax = 90;

            uniStorm.WeatherGeneration = UniStormSystem.EnableFeature.Disabled;
            uniStorm.WeatherGenerationMethod = UniStormSystem.WeatherGenerationMethodEnum.Daily;
            uniStorm.TransitionSpeed = 2;
        }

        private void ConfigureWeather(UniStormSystem uniStorm)
        {
            _availableWeather = _weatherRotation
                .Select(name => uniStorm.AllWeatherTypes.FirstOrDefault(weather => weather != null && weather.WeatherTypeName == name))
                .Where(weather => weather != null)
                .Distinct()
                .ToList();

            var startingWeather = _availableWeather.FirstOrDefault(weather => weather.WeatherTypeName == "Partly Cloudy");
            if (startingWeather == null)
                return;

            if (uniStorm.CurrentWeatherType == startingWeather)
                return;

            uniStorm.ChangeWeather(startingWeather);
        }

        private IEnumerator RunWeatherLoop(UniStormSystem uniStorm)
        {
            while (uniStorm != null)
            {
                yield return new WaitForSeconds(Random.Range(MinWeatherIntervalSeconds, MaxWeatherIntervalSeconds));

                if (uniStorm == null || _availableWeather == null || _availableWeather.Count == 0)
                    yield break;

                var nextWeather = PickNextWeather(uniStorm.CurrentWeatherType);
                if (nextWeather != null)
                    uniStorm.ChangeWeather(nextWeather);
            }
        }

        private WeatherType PickNextWeather(WeatherType currentWeather)
        {
            if (_availableWeather.Count == 1)
                return _availableWeather[0];

            WeatherType nextWeather;
            do
            {
                nextWeather = _availableWeather[Random.Range(0, _availableWeather.Count)];
            } while (nextWeather == currentWeather);

            return nextWeather;
        }
    }
}
