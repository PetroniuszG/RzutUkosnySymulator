using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BallTrajectorySimulation
{
    public partial class MainWindow : Window
    {
        // Stałe fizyczne
        private const double G = 9.81;  // przyspieszenie grawitacyjne (m/s²)

        // Parametry symulacji
        private double initialVelocity;
        private double angle;
        private double initialHeight;
        private double timeStep = 0.1;   // krok czasowy symulacji (s)
        private double scale = 1.0;      // skala wizualizacji (piksele na metr)

        // Elementy symulacji
        private Ellipse ball;
        private DispatcherTimer timer;
        private List<Point> trajectoryPoints = new List<Point>();
        private double currentTime = 0;

        // Usunięta deklaracja DropShadowEffect, ponieważ zdefiniowaliśmy go w XAML

        public MainWindow()
        {
            InitializeComponent();

            // Inicjalizacja timera
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(10);
            timer.Tick += Timer_Tick;

            // Inicjalizacja suwaka prędkości
            SpeedSlider.ValueChanged += SpeedSlider_ValueChanged;
            UpdateSpeedText();

            // Inicjalizacja układu współrzędnych
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Rysowanie układu współrzędnych
            DrawCoordinateSystem();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Przebudowanie układu współrzędnych po zmianie rozmiaru okna
            RedrawCoordinateSystem();

            // Jeśli symulacja jest aktywna, dostosuj skalę
            if (ball != null && TrajectoryCanvas.Children.Contains(ball))
            {
                AdjustScale();
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Walidacja wprowadzanych danych - tylko liczby i kropka
            Regex regex = new Regex("[^0-9.,]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void SimulateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Pobranie parametrów z pól tekstowych
                if (!double.TryParse(VelocityTextBox.Text.Replace(',', '.'), out initialVelocity) || initialVelocity <= 0)
                {
                    MessageBox.Show("Prędkość początkowa musi być liczbą dodatnią.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!double.TryParse(AngleTextBox.Text.Replace(',', '.'), out angle) || angle < 0 || angle > 90)
                {
                    MessageBox.Show("Kąt rzutu musi być liczbą z zakresu 0-90 stopni.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!double.TryParse(InitialHeightTextBox.Text.Replace(',', '.'), out initialHeight) || initialHeight < 0)
                {
                    MessageBox.Show("Wysokość początkowa musi być liczbą nieujemną.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Resetowanie symulacji
                ResetSimulation();

                // Obliczenie parametrów trajektorii
                CalculateTrajectoryParameters();

                // Dostosowanie skali
                AdjustScale();

                // Utworzenie piłki
                CreateBall();

                // Uruchomienie symulacji
                StartSimulation();

                // Aktualizacja statusu
                StatusText.Text = "Symulacja w trakcie";
                StatusText.Foreground = new SolidColorBrush(Colors.Blue);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wystąpił błąd: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetSimulation();

            // Resetowanie pól informacyjnych
            MaxHeightText.Text = "---";
            RangeText.Text = "---";
            FlightTimeText.Text = "---";

            // Aktualizacja statusu
            StatusText.Text = "Gotowy";
            StatusText.Foreground = new SolidColorBrush(Colors.Green);
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateSpeedText();

            // Aktualizacja interwału timera, jeśli timer jest aktywny
            if (timer.IsEnabled)
            {
                timer.Stop();
                timer.Interval = TimeSpan.FromMilliseconds(10 / SpeedSlider.Value);
                timer.Start();
            }
        }

        private void UpdateSpeedText()
        {
            SpeedText.Text = $"{SpeedSlider.Value:F1}x";
        }

        private void DrawCoordinateSystem()
        {
            double canvasWidth = TrajectoryCanvas.ActualWidth;
            double canvasHeight = TrajectoryCanvas.ActualHeight;

            // Rysowanie osi X
            Line xAxis = new Line
            {
                X1 = 0,
                Y1 = canvasHeight - 20,
                X2 = canvasWidth,
                Y2 = canvasHeight - 20,
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 1,
                Tag = "coordinate-system"
            };
            TrajectoryCanvas.Children.Add(xAxis);

            // Rysowanie osi Y
            Line yAxis = new Line
            {
                X1 = 20,
                Y1 = 0,
                X2 = 20,
                Y2 = canvasHeight,
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 1,
                Tag = "coordinate-system"
            };
            TrajectoryCanvas.Children.Add(yAxis);

            // Dodanie etykiet osi X
            TextBlock xLabel = new TextBlock
            {
                Text = "x [m]",
                FontSize = 12,
                Margin = new Thickness(canvasWidth - 30, canvasHeight - 40, 0, 0),
                Tag = "coordinate-system"
            };
            TrajectoryCanvas.Children.Add(xLabel);

            // Dodanie etykiet osi Y
            TextBlock yLabel = new TextBlock
            {
                Text = "y [m]",
                FontSize = 12,
                Margin = new Thickness(5, 5, 0, 0),
                Tag = "coordinate-system"
            };
            TrajectoryCanvas.Children.Add(yLabel);
        }

        private void RedrawCoordinateSystem()
        {
            // Usunięcie wszystkich elementów układu współrzędnych
            List<UIElement> elementsToRemove = new List<UIElement>();

            foreach (UIElement uiElement in TrajectoryCanvas.Children)
            {
                if (uiElement is FrameworkElement element && element.Tag != null &&
                    (element.Tag.ToString() == "coordinate-system" ||
                     element.Tag.ToString() == "tick" ||
                     element.Tag.ToString() == "tick-label"))
                {
                    elementsToRemove.Add(uiElement);
                }
            }

            foreach (UIElement element in elementsToRemove)
            {
                TrajectoryCanvas.Children.Remove(element);
            }

            // Narysowanie nowego układu współrzędnych
            DrawCoordinateSystem();

            // Zaktualizowanie podziałek, jeśli została już ustalona skala
            if (scale > 0)
            {
                DrawScaleTicks();
            }
        }

        private void DrawScaleTicks()
        {
            double canvasWidth = TrajectoryCanvas.ActualWidth;
            double canvasHeight = TrajectoryCanvas.ActualHeight;
            double xAxisY = canvasHeight - 20;
            double yAxisX = 20;

            // Usunięcie istniejących podziałek
            List<UIElement> elementsToRemove = new List<UIElement>();
            foreach (UIElement uiElement in TrajectoryCanvas.Children)
            {
                if (uiElement is FrameworkElement element && element.Tag != null &&
                    (element.Tag.ToString() == "tick" ||
                     element.Tag.ToString() == "tick-label"))
                {
                    elementsToRemove.Add(uiElement);
                }
            }

            foreach (UIElement element in elementsToRemove)
            {
                TrajectoryCanvas.Children.Remove(element);
            }

            // Rysowanie podziałek na osi X
            int xTickCount = (int)(canvasWidth / (50 * scale));
            for (int i = 1; i <= xTickCount; i++)
            {
                double x = yAxisX + i * 50 * scale;
                if (x >= canvasWidth) break;

                // Linia podziałki
                Line tick = new Line
                {
                    X1 = x,
                    Y1 = xAxisY - 5,
                    X2 = x,
                    Y2 = xAxisY + 5,
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 1,
                    Tag = "tick"
                };
                TrajectoryCanvas.Children.Add(tick);

                // Etykieta podziałki
                TextBlock label = new TextBlock
                {
                    Text = (i * 50).ToString(),
                    FontSize = 10,
                    Margin = new Thickness(x - 10, xAxisY + 5, 0, 0),
                    Tag = "tick-label"
                };
                TrajectoryCanvas.Children.Add(label);
            }

            // Rysowanie podziałek na osi Y
            int yTickCount = (int)(canvasHeight / (50 * scale));
            for (int i = 1; i <= yTickCount; i++)
            {
                double y = xAxisY - i * 50 * scale;
                if (y <= 0) break;

                // Linia podziałki
                Line tick = new Line
                {
                    X1 = yAxisX - 5,
                    Y1 = y,
                    X2 = yAxisX + 5,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 1,
                    Tag = "tick"
                };
                TrajectoryCanvas.Children.Add(tick);

                // Etykieta podziałki
                TextBlock label = new TextBlock
                {
                    Text = (i * 50).ToString(),
                    FontSize = 10,
                    Margin = new Thickness(2, y - 7, 0, 0),
                    Tag = "tick-label"
                };
                TrajectoryCanvas.Children.Add(label);
            }
        }

        private void CreateBall()
        {
            ball = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(Colors.Red),
                Stroke = new SolidColorBrush(Colors.DarkRed),
                StrokeThickness = 1
            };

            // Umieszczenie piłki w punkcie początkowym
            Canvas.SetLeft(ball, 20 - ball.Width / 2);
            Canvas.SetTop(ball, TrajectoryCanvas.ActualHeight - 20 - initialHeight * scale - ball.Height / 2);

            TrajectoryCanvas.Children.Add(ball);
        }

        private void StartSimulation()
        {
            // Resetowanie czasu symulacji
            currentTime = 0;
            trajectoryPoints.Clear();

            // Ustawienie interwału timera
            timer.Interval = TimeSpan.FromMilliseconds(10 / SpeedSlider.Value);

            // Rozpoczęcie symulacji
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Konwersja kąta z stopni na radiany
            double angleRadians = angle * Math.PI / 180;

            // Obliczenie składowych prędkości początkowej
            double v0x = initialVelocity * Math.Cos(angleRadians);
            double v0y = initialVelocity * Math.Sin(angleRadians);

            // Obliczenie pozycji dla aktualnego czasu
            double x = v0x * currentTime;
            double y = initialHeight + v0y * currentTime - 0.5 * G * currentTime * currentTime;

            // Konwersja współrzędnych do układu canvas
            double canvasX = 20 + x * scale;
            double canvasY = TrajectoryCanvas.ActualHeight - 20 - y * scale;

            // Sprawdzenie, czy piłka uderzyła w ziemię
            if (y < 0)
            {
                timer.Stop();

                // Aktualizacja statusu
                StatusText.Text = "Zakończono";
                StatusText.Foreground = new SolidColorBrush(Colors.Green);

                return;
            }

            // Aktualizacja pozycji piłki
            Canvas.SetLeft(ball, canvasX - ball.Width / 2);
            Canvas.SetTop(ball, canvasY - ball.Height / 2);

            // Dodanie punktu do trajektorii
            trajectoryPoints.Add(new Point(canvasX, canvasY));

            // Narysowanie punktu trajektorii
            Ellipse point = new Ellipse
            {
                Width = 3,
                Height = 3,
                Fill = new SolidColorBrush(Colors.Blue),
                Stroke = new SolidColorBrush(Colors.DarkBlue),
                StrokeThickness = 0.5
            };

            Canvas.SetLeft(point, canvasX - point.Width / 2);
            Canvas.SetTop(point, canvasY - point.Height / 2);

            TrajectoryCanvas.Children.Add(point);

            // Inkrementacja czasu symulacji
            currentTime += timeStep;
        }

        private void ResetSimulation()
        {
            // Zatrzymanie timera
            timer.Stop();

            // Wyczyszczenie punktów trajektorii
            trajectoryPoints.Clear();

            // Usunięcie piłki i punktów trajektorii z canvas
            List<UIElement> elementsToRemove = new List<UIElement>();

            foreach (UIElement element in TrajectoryCanvas.Children)
            {
                if (element is Ellipse)
                {
                    elementsToRemove.Add(element);
                }
            }

            foreach (UIElement element in elementsToRemove)
            {
                TrajectoryCanvas.Children.Remove(element);
            }

            // Odtworzenie układu współrzędnych
            RedrawCoordinateSystem();

            // Resetowanie czasu symulacji
            currentTime = 0;
        }

        private void AdjustScale()
        {
            // Obliczenie teoretycznego maksymalnego zasięgu i wysokości
            double angleRadians = angle * Math.PI / 180;
            double maxRange = (initialVelocity * Math.Cos(angleRadians) / G) *
                             (initialVelocity * Math.Sin(angleRadians) + Math.Sqrt(Math.Pow(initialVelocity * Math.Sin(angleRadians), 2) + 2 * G * initialHeight));

            double maxHeight = initialHeight + Math.Pow(initialVelocity * Math.Sin(angleRadians), 2) / (2 * G);

            // Obliczenie skali tak, aby trajektoria zmieściła się na canvas
            double xScale = (TrajectoryCanvas.ActualWidth - 40) / maxRange;
            double yScale = (TrajectoryCanvas.ActualHeight - 40) / maxHeight;

            // Wybór mniejszej skali
            scale = Math.Min(xScale, yScale) * 0.9; // 90% wartości dla zapewnienia marginesu

            // Aktualizacja informacji o skali
            ScaleInfoText.Text = $"Skala: 1 px = {1 / scale:F2} m";

            // Aktualizacja podziałek na osiach
            DrawScaleTicks();
        }

        private void CalculateTrajectoryParameters()
        {
            double angleRadians = angle * Math.PI / 180;

            // Składowe prędkości początkowej
            double v0x = initialVelocity * Math.Cos(angleRadians);
            double v0y = initialVelocity * Math.Sin(angleRadians);

            // Czas lotu
            double flightTime = (v0y + Math.Sqrt(v0y * v0y + 2 * G * initialHeight)) / G;

            // Maksymalna wysokość
            double maxHeight = initialHeight + v0y * v0y / (2 * G);

            // Zasięg rzutu
            double range = v0x * flightTime;

            // Aktualizacja pól informacyjnych
            MaxHeightText.Text = $"{maxHeight:F2} m";
            RangeText.Text = $"{range:F2} m";
            FlightTimeText.Text = $"{flightTime:F2} s";
        }
    }
}