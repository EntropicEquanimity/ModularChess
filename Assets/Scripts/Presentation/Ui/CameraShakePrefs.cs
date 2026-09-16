using UnityEngine;

namespace ModularChess.Presentation
{
    public static class CameraShakePrefs
    {
        #region Fields
        public const string Key = "CameraShake";
        public const int Min = 0;
        public const int Max = 100;
        public const int Default = 100;
        public static int SliderValue
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(Key, Default), Min, Max);
            set
            {
                PlayerPrefs.SetInt(Key, Mathf.Clamp(value, Min, Max));
                PlayerPrefs.Save();
            }
        }
        public static float Multiplier => SliderValue / (float)Max;
        #endregion
    }
}
