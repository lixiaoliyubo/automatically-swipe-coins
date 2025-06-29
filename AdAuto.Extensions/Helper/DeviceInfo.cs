using AdvancedSharpAdbClient.Models;
using System;

namespace AdAuto.Extensions.Helper
{
    public class DeviceInfo
    {
        public string Serial { get; set; }
        public string Name { get; set; }
        public bool IsConnected { get; set; }
        public string Product { get; set; }
        public string Model { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public DeviceData Data { get; set; }

        public DeviceInfo(string serial, string name, string product = "", string model = "", int width = 0, int height = 0)
        {
            Serial = serial;
            Name = name;
            Product = product;
            Model = model;
            Width = width;
            Height = height;
            IsConnected = true;
        }

        public override string ToString()
        {
            return $"{Name} ({Serial})";
        }
    }
}