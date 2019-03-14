﻿using System;
using System.Windows;
using System.Windows.Media;
using Xky.UI.Data;

namespace Xky.UI.Controls.Panel
{
    public class CirclePanel : System.Windows.Controls.Panel
    {
        public static readonly DependencyProperty DiameterProperty = DependencyProperty.Register(
            "Diameter", typeof(double), typeof(CirclePanel), new FrameworkPropertyMetadata(170.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public double Diameter
        {
            get => (double) GetValue(DiameterProperty);
            set => SetValue(DiameterProperty, value);
        }

        public static readonly DependencyProperty KeepVerticalProperty = DependencyProperty.Register(
            "KeepVertical", typeof(bool), typeof(CirclePanel), new FrameworkPropertyMetadata(ValueBoxes.FalseBox, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public bool KeepVertical
        {
            get => (bool) GetValue(KeepVerticalProperty);
            set => SetValue(KeepVerticalProperty, value);
        }

        public static readonly DependencyProperty OffsetAngleProperty = DependencyProperty.Register(
            "OffsetAngle", typeof(double), typeof(CirclePanel), new FrameworkPropertyMetadata(ValueBoxes.Double0Box, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public double OffsetAngle
        {
            get => (double) GetValue(OffsetAngleProperty);
            set => SetValue(OffsetAngleProperty, value);
        }

        // ReSharper disable once RedundantAssignment
        protected override Size MeasureOverride(Size availableSize)
        {
            if (Children.Count == 0) return new Size(Diameter, Diameter);

            availableSize = new Size(Diameter, Diameter);
            var i = 0;
            var perDeg = 360.0 / Children.Count;
            var radius = Diameter / 2;
            foreach (UIElement element in Children)
            {
                element.Measure(availableSize);
                var centerX = element.DesiredSize.Width / 2.0;
                var centerY = element.DesiredSize.Height / 2.0;
                var angle = perDeg * i++ + OffsetAngle;
                var transform = new RotateTransform
                {
                    CenterX = centerX,
                    CenterY = centerY,
                    Angle = KeepVertical ? 0 : angle
                };
                element.RenderTransform = transform;
                var r = Math.PI * angle / 180.0;
                var x = radius * Math.Cos(r);
                var y = radius * Math.Sin(r);
                var rectX = x + availableSize.Width / 2 - centerX;
                var rectY = y + availableSize.Height / 2 - centerY;
                element.Arrange(new Rect(rectX, rectY, element.DesiredSize.Width, element.DesiredSize.Height));
            }
            return availableSize;
        }
    }
}