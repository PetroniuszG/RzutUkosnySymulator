using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

//==============================================================================
// Symulacja Rzutu Ukośnego
//------------------------------------------------------------------------------
// Plik:         MainWindow.xaml.cs
// Wersja:       1.0.0
// Data:         05.03.2025
// Autor:        Jakub Prabucki
//==============================================================================
// Opis:
//   Aplikacja symulująca lot piłki w rzucie ukośnym na różnych planetach.
//   Pozwala na badanie wpływu różnych parametrów (prędkość początkowa, kąt, 
//   wysokość początkowa, grawitacja) na trajektorię lotu. Wizualizacja 
//   dynamicznie zmienia rozmiar na podstawie wartości wprowadzonych przez
//   użytkownika.
//
// Funkcjonalności:
//   - Symulacja lotu piłki z animacją w czasie rzeczywistym
//   - Zmienna prędkość animacji z regulacją
//   - Wybór planety determinujący grawitację i wygląd
//   - Dynamiczne skalowanie jednostek (mm, cm, m, km, Mm)
//   - Interaktywny układ współrzędnych z adaptacyjnymi podziałkami
//   - Obliczanie i wyświetlanie parametrów rzutu (maksymalna wysokość,
//     zasięg, czas lotu)
//
//==============================================================================

namespace BallTrajectorySimulation
{
    /// <summary>
    /// Klasa planety, przechowująca informacje o nazwie, grawitacji i wyglądzie.
    /// </summary>
    public class Planet
    {
        
        //Nazwa planety
        public string Name { get; set; }

        //Grawitacja na planecie.
        public double Gravity { get; set; }

        //Kolor tła dla planet.
        public Brush BackgroundColor { get; set; }

        //Kolor linii siatki układu dla planet.
        public Color GridLineColor { get; set; }

        /// <summary>
        /// Inicjalizuje nową instancję planety z określonymi parametrami.
        /// </summary>
        /// <param name="name">Nazwa planety.</param>
        /// <param name="gravity">Przyspieszenie grawitacyjne (m/s²).</param>
        /// <param name="backgroundColor">Kolor tła reprezentujący atmosferę.</param>
        /// <param name="gridLineColor">Kolor linii siatki układu współrzędnych.</param>
        public Planet(string name, double gravity, Brush backgroundColor, Color gridLineColor)
        {
            Name = name;
            Gravity = gravity;
            BackgroundColor = backgroundColor;
            GridLineColor = gridLineColor;
        }

        /// <summary>
        /// Zwraca nazwę planety jako string.
        /// </summary>
        /// <returns>String - nazwa planety.</returns>
        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Główne okno aplikacji.
    /// </summary>
    public partial class MainWindow : Window
    {
        //Stałe fizyczne
        //Domyślne przyspieszenie grawitacyjne (Ziemia).
        private const double G = 9.81;

        //Parametry symulacji
        //Prędkość początkowa.
        private double initialVelocity;

        //Kąt rzutu.
        private double angle;

        //Wysokość początkowa.
        private double initialHeight;

        //Krok czasowy symulacji.
        private double timeStep = 0.1;

        //Skala wizualizacji.
        private double scale = 1.0;

        //Elementy symulacji
        //Piłka reprezentująca lot.
        private Ellipse ball;

        //Timer zarządzający animacją symulacji.
        private DispatcherTimer timer;

        //Kolekcja punktów śledzących trajektorię lotu.
        private List<Point> trajectoryPoints = new List<Point>();

        //Aktualny czas symulacji, od jej rozpoczęcia.
        private double currentTime = 0;

        //Lista dostępnych planet do wyboru w symulacji.
        private List<Planet> planets;

        //Aktualnie wybrana planeta, determinująca przyspieszenie grawitacyjne i wygląd.
        private Planet currentPlanet;

        /// <summary>
        /// Inicjalizuje główne okno aplikacji i wszystkie jego komponenty.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            //Inicjalizacja planet
            InitializePlanets();

            //Inicjalizacja timera
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(10);
            timer.Tick += Timer_Tick;

            //Inicjalizacja suwaka prędkości
            SpeedSlider.ValueChanged += SpeedSlider_ValueChanged;
            UpdateSpeedText();

            //Inicjalizacja układu współrzędnych po załadowaniu okna
            Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// Inicjalizuje listę planet dostępnych w symulacji, wraz z ich parametrami fizycznymi i wizualnymi.
        /// </summary>
        private void InitializePlanets()
        {
            //Utworzenie i wprowadzenie wartosci dla listy planet
            planets = new List<Planet>
            {
                new Planet("Ziemia", 9.81, new SolidColorBrush(Color.FromRgb(230, 240, 255)), Colors.Black),
                new Planet("Księżyc", 1.62, new SolidColorBrush(Color.FromRgb(210, 210, 210)), Colors.DarkGray),
                new Planet("Mars", 3.72, new SolidColorBrush(Color.FromRgb(255, 200, 180)), Colors.Brown),
                new Planet("Wenus", 8.87, new SolidColorBrush(Color.FromRgb(255, 220, 150)), Colors.Orange),
                new Planet("Jowisz", 24.79, new SolidColorBrush(Color.FromRgb(245, 225, 180)), Colors.SaddleBrown),
                new Planet("Merkury", 3.7, new SolidColorBrush(Color.FromRgb(200, 200, 200)), Colors.Gray)
            };

            PlanetComboBox.ItemsSource = planets;
            PlanetComboBox.SelectedIndex = 0; //Ziemia jako domyślna
            currentPlanet = planets[0];
        }

        /// <summary>
        /// Obsługuje zdarzenie załadowania okna, inicjalizując układ współrzędnych i tło canvas.
        /// </summary>
        /// <param name="sender">Obiekt wywołujący zdarzenie.</param>
        /// <param name="e">Argumenty zdarzenia.</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //Rysowanie układu współrzędnych
            DrawCoordinateSystem();

            //Aktualizacja tła canvasu na podstawie wybranej planety
            UpdateCanvasBackground();
        }

        /// <summary>
        /// Obsługuje zdarzenie zmiany rozmiaru okna, dostosowując układ współrzędnych i skalę.
        /// </summary>
        /// <param name="sender">Obiekt wywołujący zdarzenie.</param>
        /// <param name="e">Argumenty zdarzenia z informacjami o nowych wymiarach.</param>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //Przebudowanie układu współrzędnych po zmianie rozmiaru okna
            RedrawCoordinateSystem();

            //Jeśli symulacja jest aktywna, dostosuj skalę
            if (ball != null && TrajectoryCanvas.Children.Contains(ball))
            {
                AdjustScale();
            }
        }

        /// <summary>
        /// Obsługuje zdarzenie wyboru nowej planety z listy, aktualizując tło i parametry symulacji.
        /// </summary>
        /// <param name="sender">Obiekt wywołujący zdarzenie.</param>
        /// <param name="e">Argumenty zdarzenia z informacjami o wybranym elemencie.</param>
        private void PlanetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentPlanet = (Planet)PlanetComboBox.SelectedItem;
            UpdateCanvasBackground();

            //Jeśli symulacja jest aktywna, aktualizuj parametry
            if (timer != null && timer.IsEnabled)
            {
                ResetSimulation();
                CalculateTrajectoryParameters();
                AdjustScale();
                CreateBall();
                StartSimulation();
            }
        }

        /// <summary>
        /// Aktualizuje kolor tła canvas zgodnie z kolorem wybranej planety.
        /// </summary>
        /// <remarks>
        /// W przypadku błędu aktualizacji, wyjątek jest przechwytywany i logowany, 
        /// aby nie przerwać działania aplikacji.
        /// </remarks>
        private void UpdateCanvasBackground()
        {
            try
            {
                //Aktualizacja koloru tła canvasa bezpośrednio
                TrajectoryCanvas.Background = currentPlanet.BackgroundColor;

                //Aktualizacja kolorów osi współrzędnych
                RedrawCoordinateSystem();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd aktualizacji tła: {ex.Message}");
            }
        }

        /// <summary>
        /// Waliduje dane wprowadzane do pól tekstowych, dopuszczając tylko liczby i znaki dziesiętne.
        /// </summary>
        /// <param name="sender">Obiekt wywołujący zdarzenie.</param>
        /// <param name="e">Argumenty zdarzenia z informacjami o wprowadzonym tekście.</param>
        /// <remarks>
        /// Metoda używa wyrażeń regularnych do zapewnienia, że wprowadzane są tylko dozwolone znaki,
        /// co zapobiega wprowadzaniu nieprawidłowych danych.
        /// </remarks>
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Walidacja wprowadzanych danych - tylko liczby i kropka/przecinek
            Regex regex = new Regex("[^0-9.,]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// Obsługa przycisku symulacji.
        /// </summary>
        /// <param name="sender">Obiekt wywołujący zdarzenie.</param>
        /// <param name="e">Argumenty zdarzenia.</param>
        /// <remarks>
        /// Metoda pobiera i waliduje parametry, ostrzega o potencjalnie skrajnych wartościach,
        /// a następnie inicjuje symulację. W przypadku błędu wyświetla komunikat.
        /// </remarks>
        /// <exception cref="FormatException">Może wystąpić przy próbie konwersji nieprawidłowych danych wejściowych.</exception>
        private void SimulateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!double.TryParse(InitialHeightTextBox.Text.Replace(',', '.'), out initialHeight) || initialHeight < 0)
                {
                    MessageBox.Show("Wysokość początkowa musi być liczbą nieujemną.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                //Pobranie parametrów z pól tekstowych i walidacja
                if (!double.TryParse(VelocityTextBox.Text.Replace(',', '.'), out initialVelocity) || initialVelocity <= 0)
                {
                    MessageBox.Show("Wprowadź poprawną prędkość.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!double.TryParse(AngleTextBox.Text.Replace(',', '.'), out angle) || angle < 0 || angle > 90)
                {
                    MessageBox.Show("Kąt musi mieć wartość od 0 do 90 stopni.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                //Sprawdzanie ekstremalnych wartości
                if (initialVelocity > 1000)
                {
                    string message = "Wprowadzona prędkość jest bardzo wysoka. \n\nCzy chcesz kontynuować?";
                    MessageBoxResult result = MessageBox.Show(message, "Ostrzeżene - duża wartość.", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    //jesli nie przerwij
                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                if (initialHeight > 1000)
                {
                    string message = "Wprowadzona wysokość jest bardzo wysoka.\n\nCzy chcesz kontynuować?";
                    MessageBoxResult result = MessageBox.Show(message, "Ostrzeżenie - duża wartość.", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    //jesli nie przerwij
                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                //Resetowanie symulacji
                ResetSimulation();

                //Obliczenie parametrów trajektorii
                CalculateTrajectoryParameters();

                //Dostosowanie skali
                AdjustScale();

                //Utworzenie piłki
                CreateBall();

                //Uruchomienie symulacji
                StartSimulation();

                //Aktualizacja statusu
                StatusText.Text = "W trakcie";
                StatusText.Foreground = new SolidColorBrush(Colors.Blue);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Obsługuje kliknięcie przycisku resetowania, zatrzymując symulację i czyszcząc obszar rysowania.
        /// </summary>
        /// <param name="sender">Obiekt wywołujący zdarzenie.</param>
        /// <param name="e">Argumenty zdarzenia.</param>
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetSimulation();
            //Resetowanie pól informacyjnych oraz wprowadzania
            MaxHeightText.Text = "---";
            RangeText.Text = "---";
            FlightTimeText.Text = "---";
            VelocityTextBox.Text = "";
            AngleTextBox.Text = "";
            InitialHeightTextBox.Text = "0";

            // Aktualizacja statusu
            StatusText.Text = "Gotowy";
            StatusText.Foreground = new SolidColorBrush(Colors.Green);
        }

        /// <summary>
        /// Obsługuje zmianę wartości suwaka prędkości, aktualizując wyświetlany tekst i parametry symulacji.
        /// </summary>
        /// <param name="sender">Obiekt wywołujący zdarzenie.</param>
        /// <param name="e">Argumenty zdarzenia z informacjami o nowej i starej wartości.</param>
        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateSpeedText();

            //Aktualizacja interwału timera, jeśli timer jest aktywny
            if (timer != null && timer.IsEnabled)
            {
                timer.Stop();
                timer.Interval = TimeSpan.FromMilliseconds(10 / SpeedSlider.Value);
                timeStep = 0.1 * SpeedSlider.Value; //Dostosowanie kroku czasowego do prędkości
                timer.Start();
            }
        }

        /// <summary>
        /// Aktualizuje tekst wyświetlający aktualną wartość mnożnika prędkości.
        /// </summary>
        private void UpdateSpeedText()
        {
            SpeedText.Text = $"{SpeedSlider.Value:F1}x";
        }

        /// <summary>
        /// Rysuje podstawowy układ współrzędnych na canvas, z osiami X i Y oraz etykietami.
        /// </summary>
        private void DrawCoordinateSystem()
        {
            double canvasWidth = TrajectoryCanvas.ActualWidth;
            double canvasHeight = TrajectoryCanvas.ActualHeight;

            //Kolor linii zależny od planety
            Brush lineBrush = new SolidColorBrush(currentPlanet?.GridLineColor ?? Colors.Black);

            //Rysowanie osi X
            Line xAxis = new Line
            {
                X1 = 0,
                Y1 = canvasHeight - 20,
                X2 = canvasWidth,
                Y2 = canvasHeight - 20,
                Stroke = lineBrush,
                StrokeThickness = 1,
                Tag = "coordinate-system"
            };
            TrajectoryCanvas.Children.Add(xAxis);

            //Rysowanie osi Y
            Line yAxis = new Line
            {
                X1 = 20,
                Y1 = 0,
                X2 = 20,
                Y2 = canvasHeight,
                Stroke = lineBrush,
                StrokeThickness = 1,
                Tag = "coordinate-system"
            };
            TrajectoryCanvas.Children.Add(yAxis);

            //Dodanie etykiet osi X
            TextBlock xLabel = new TextBlock
            {
                Text = "x [m]",
                FontSize = 12,
                Foreground = lineBrush,
                Margin = new Thickness(canvasWidth - 30, canvasHeight - 40, 0, 0),
                Tag = "coordinate-system"
            };
            TrajectoryCanvas.Children.Add(xLabel);

            //Dodanie etykiet osi Y
            TextBlock yLabel = new TextBlock
            {
                Text = "y [m]",
                FontSize = 12,
                Foreground = lineBrush,
                Margin = new Thickness(35, 5, 0, 0),
                Tag = "coordinate-system"
            };
            TrajectoryCanvas.Children.Add(yLabel);
        }

        /// <summary>
        /// Usuwa i ponownie rysuje cały układ współrzędnych, używane przy zmianie rozmiaru okna lub planety.
        /// </summary>
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

        /// <summary>
        /// Rysuje podziałki na osiach układu współrzędnych z adaptacyjnymi jednostkami.
        /// </summary>
        /// <remarks>
        /// Metoda automatycznie dostosowuje jednostki (mm, cm, m, km) i format wyświetlania
        /// w zależności od skali, aby zapewnić czytelność oznaczeń.
        /// </remarks>
        private void DrawScaleTicks()
        {
            double canvasWidth = TrajectoryCanvas.ActualWidth;
            double canvasHeight = TrajectoryCanvas.ActualHeight;
            double xAxisY = canvasHeight - 20;
            double yAxisX = 20;

            //Kolor linii zależny od planety
            Brush lineBrush = new SolidColorBrush(currentPlanet?.GridLineColor ?? Colors.Black);

            //Usunięcie istniejących podziałek
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

            //Określenie jednostki i współczynnika konwersji dla podziałek
            string unit = "m";
            double conversionFactor = 1.0;
            double valueThreshold = 1.0; // Próg zaokrąglania wartości

            //Wartości w metrach
            double maxRangeInMeters = canvasWidth / scale;

            if (maxRangeInMeters > 1000)
            {
                unit = "km";
                conversionFactor = 0.001; //m -> km
                valueThreshold = 0.1; //Zaokrąglaj do 0.1 km
            }
            else if (maxRangeInMeters < 0.1)
            {
                unit = "cm";
                conversionFactor = 100.0; // m -> cm
                valueThreshold = 1.0; //Zaokrąglaj do 1 cm
            }

            //Określenie optymalnych odstępów między podziałkami
            int numTicks = 8; //Docelowa liczba podziałek
            double meterStep = Math.Max(1, Math.Round((canvasWidth / scale) / numTicks / valueThreshold)) * valueThreshold;
            double pixelStep = meterStep * scale;

            //Rysowanie podziałek na osi X
            for (double x = yAxisX + pixelStep; x < canvasWidth; x += pixelStep)
            {
                // Linia podziałki
                Line tick = new Line
                {
                    X1 = x,
                    Y1 = xAxisY - 5,
                    X2 = x,
                    Y2 = xAxisY + 5,
                    Stroke = lineBrush,
                    StrokeThickness = 1,
                    Tag = "tick"
                };
                TrajectoryCanvas.Children.Add(tick);

                //Etykieta podziałki - z uwzględnieniem konwersji jednostek
                double meterValue = (x - yAxisX) / scale;
                double displayValue = meterValue * conversionFactor;

                //Formatowanie wartości
                string formattedValue;
                if (displayValue >= 10000)
                {
                    //Dla bardzo dużych wartości wyświetl w km z jednym miejscem po przecinku
                    formattedValue = (displayValue / 1000).ToString("F1") + " km";
                    //Zmień jednostkę na kilometr
                    unit = "km";
                }
                else if (displayValue >= 1000)
                {
                    //Dla dużych wartości wyświetl w km z jednym miejscem po przecinku
                    formattedValue = (displayValue / 1000).ToString("F1") + " km";
                }
                else if (displayValue >= 100)
                {
                    //Dla średnich wartości bez miejsc po przecinku
                    formattedValue = displayValue.ToString("F0") + " " + unit;
                }
                else if (displayValue >= 10)
                {
                    //Dla mniejszych wartości jedno miejsce po przecinku
                    formattedValue = displayValue.ToString("F1") + " " + unit;
                }
                else
                {
                    //Dla bardzo małych wartości dwa miejsca po przecinku
                    formattedValue = displayValue.ToString("F2") + " " + unit;
                }

                TextBlock label = new TextBlock
                {
                    Text = formattedValue,
                    FontSize = 10,
                    Foreground = lineBrush,
                    Margin = new Thickness(x - 20, xAxisY + 5, 0, 0),
                    Tag = "tick-label"
                };
                TrajectoryCanvas.Children.Add(label);
            }

            //Rysowanie podziałek na osi Y
            for (double y = xAxisY - pixelStep; y > 0; y -= pixelStep)
            {
                //Linia podziałki
                Line tick = new Line
                {
                    X1 = yAxisX - 5,
                    Y1 = y,
                    X2 = yAxisX + 5,
                    Y2 = y,
                    Stroke = lineBrush,
                    StrokeThickness = 1,
                    Tag = "tick"
                };
                TrajectoryCanvas.Children.Add(tick);

                //Etykieta podziałki - z uwzględnieniem konwersji jednostek
                double meterValue = (xAxisY - y) / scale;
                double displayValue = meterValue * conversionFactor;

                //Formatowanie wartości, aby uniknąć notacji naukowej
                string formattedValue;
                if (displayValue >= 10000)
                {
                    //Dla bardzo dużych wartości wyświetl w km z jednym miejscem po przecinku
                    formattedValue = (displayValue / 1000).ToString("F1") + " km";
                    unit = "km";
                }
                else if (displayValue >= 1000)
                {
                    //Dla dużych wartości wyświetl w km z jednym miejscem po przecinku
                    formattedValue = (displayValue / 1000).ToString("F1") + " km";
                }
                else if (displayValue >= 100)
                {
                    //Dla średnich wartości bez miejsc po przecinku
                    formattedValue = displayValue.ToString("F0") + " " + unit;
                }
                else if (displayValue >= 10)
                {
                    //Dla mniejszych wartości jedno miejsce po przecinku
                    formattedValue = displayValue.ToString("F1") + " " + unit;
                }
                else
                {
                    //Dla bardzo małych wartości dwa miejsca po przecinku
                    formattedValue = displayValue.ToString("F2") + " " + unit;
                }

                TextBlock label = new TextBlock
                {
                    Text = formattedValue,
                    FontSize = 10,
                    Foreground = lineBrush,
                    Margin = new Thickness(35, y - 7, 0, 0),
                    Tag = "tick-label"
                };
                TrajectoryCanvas.Children.Add(label);
            }

            //Aktualizacja etykiet osi
            foreach (UIElement element in TrajectoryCanvas.Children)
            {
                if (element is TextBlock textBlock && element.GetValue(FrameworkElement.TagProperty) as string == "coordinate-system")
                {
                    string text = textBlock.Text;
                    if (text == "x [m]")
                        textBlock.Text = $"x [{unit}]";
                    else if (text == "y [m]")
                        textBlock.Text = $"y [{unit}]";
                }
            }
        }

        /// <summary>
        /// Tworzy i dodaje do canvasa wizualną reprezentację piłki.
        /// </summary>
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

            //Umieszczenie piłki w punkcie początkowym
            Canvas.SetLeft(ball, 20 - ball.Width / 2);
            Canvas.SetTop(ball, TrajectoryCanvas.ActualHeight - 20 - initialHeight * scale - ball.Height / 2);

            TrajectoryCanvas.Children.Add(ball);
        }

        /// <summary>
        /// Rozpoczyna symulację, inicjalizując timer i parametry animacji.
        /// </summary>
        private void StartSimulation()
        {
            //Resetowanie czasu symulacji
            currentTime = 0;
            trajectoryPoints.Clear();

            //Ustawienie interwału timera i kroku czasowego
            timer.Interval = TimeSpan.FromMilliseconds(10 / SpeedSlider.Value);
            timeStep = 0.1 * SpeedSlider.Value; //Bazowy krok czasowy * mnożnik prędkości

            //Rozpoczęcie symulacji
            timer.Start();
        }

        /// <summary>
        /// Na podstawie timera rysuje kolejne punkty trajektorii
        /// </summary>
        /// <param name="sender">Obiekt wywołujący zdarzenie.</param>
        /// <param name="e">Argumenty zdarzenia.</param>
        /// <remarks>
        /// Metoda oblicza pozycję piłki w każdym kroku czasowym na podstawie równań ruchu,
        /// uwzględniając grawitację wybranej planety. W przypadku błędu wyświetla komunikat.
        /// </remarks>
        /// <exception cref="Exception">Może wystąpić podczas obliczeń lub aktualizacji UI.</exception>
        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                //Konwersja kąta z stopni na radiany
                double angleRadians = angle * Math.PI / 180;

                //Warunki początkowe
                double v0x = initialVelocity * Math.Cos(angleRadians);
                double v0y = initialVelocity * Math.Sin(angleRadians);

                //Grawitacja planety
                double planetGravity = currentPlanet.Gravity;

                //Obliczenie pozycji dla aktualnego czasu
                double x = v0x * currentTime;
                double y = initialHeight + v0y * currentTime - 0.5 * planetGravity * currentTime * currentTime;

                //Konwersja współrzędnych do układu canvas
                double canvasX = 20 + x * scale;
                double canvasY = TrajectoryCanvas.ActualHeight - 20 - y * scale;

                //Sprawdzenie, czy piłka uderzyła w ziemię
                if (y < 0)
                {
                    //jesli tak zatrzymaj timer
                    timer.Stop();

                    //oraz zaktualizuj status
                    StatusText.Text = "Zakończono";
                    StatusText.Foreground = new SolidColorBrush(Colors.Green);

                    return;
                }

                //Aktualizacja pozycji piłki
                Canvas.SetLeft(ball, canvasX - ball.Width / 2);
                Canvas.SetTop(ball, canvasY - ball.Height / 2);

                //Dodanie punktu do trajektorii
                trajectoryPoints.Add(new Point(canvasX, canvasY));

                //Narysowanie punktu trajektorii
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

                //Inkrementacja czasu symulacji
                currentTime += timeStep;
            }
            catch (Exception ex)
            {
                timer.Stop();
                StatusText.Text = "Błąd symulacji";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
                Console.WriteLine($"Błąd w symulacji: {ex.Message}");
            }
        }

        /// <summary>
        /// Resetuje symulację, czyszcząc canvas i zatrzymując timer.
        /// </summary>
        /// <remarks>
        /// Metoda usuwa wszystkie elementy związane z symulacją (piłkę i punkty trajektorii) 
        /// i przywraca początkowy stan aplikacji wraz z układem współrzędnych.
        /// </remarks>
        private void ResetSimulation()
        {
            //Zatrzymanie timera
            if (timer != null)
            {
                timer.Stop();
            }

            //Wyczyszczenie punktów trajektorii
            trajectoryPoints.Clear();

            //Usunięcie piłki i punktów trajektorii z canvas
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

            //Odtworzenie układu współrzędnych
            RedrawCoordinateSystem();

            //Resetowanie czasu symulacji
            currentTime = 0;
        }

        /// <summary>
        /// Dostosowuje skalę wizualizacji, aby trajektoria zmieściła się na canvas.
        /// </summary>
        /// <remarks>
        /// Metoda oblicza maksymalny zasięg i wysokość trajektorii na podstawie parametrów fizycznych,
        /// a następnie dopasowuje skalę, aby zapewnić właściwe wyświetlanie. Dodatkowo aktualizuje
        /// informację o skali i podziałki na osiach współrzędnych.
        /// </remarks>
        private void AdjustScale()
        {
            //Obliczenie teoretycznego maksymalnego zasięgu i wysokości
            double angleRadians = angle * Math.PI / 180;
            double planetGravity = currentPlanet.Gravity;

            double maxRange = (initialVelocity * Math.Cos(angleRadians) / planetGravity) *
                             (initialVelocity * Math.Sin(angleRadians) + Math.Sqrt(Math.Pow(initialVelocity * Math.Sin(angleRadians), 2) + 2 * planetGravity * initialHeight));

            double maxHeight = initialHeight + Math.Pow(initialVelocity * Math.Sin(angleRadians), 2) / (2 * planetGravity);

            //Obliczenie skali tak, aby trajektoria zmieściła się na canvas
            double xScale = (TrajectoryCanvas.ActualWidth - 40) / maxRange;
            double yScale = (TrajectoryCanvas.ActualHeight - 40) / maxHeight;

            //Wybór mniejszej skali
            scale = Math.Min(xScale, yScale) * 0.9;

            //Sprawdzenie czy skala jest bardzo mała
            if (1 / scale > 1000)
            {
                // Dla bardzo dużych trajektorii zmieniamy jednostkę na kilometry
                string unit = "km";
                double displayScale = 1 / (scale * 1000); //Konwersja m -> km
                ScaleInfoText.Text = $"Skala: 1 px = {displayScale:F2} {unit}";
            }
            else if (1 / scale < 0.1)
            {
                //Dla bardzo małych trajektorii zmieniamy jednostkę na centymetry
                string unit = "cm";
                double displayScale = 100 / scale; // Konwersja m -> cm
                ScaleInfoText.Text = $"Skala: 1 px = {displayScale:F2} {unit}";
            }
            else
            {
                //Standardowa jednostka - metry
                ScaleInfoText.Text = $"Skala: 1 px = {1 / scale:F2} m";
            }

            //Aktualizacja podziałek na osiach
            DrawScaleTicks();
        }

        /// <summary>
        /// Oblicza i wyświetla parametry trajektorii na podstawie aktualnych wartości.
        /// </summary>
        /// <remarks>
        /// Metoda oblicza maksymalną wysokość, zasięg i czas lotu na podstawie parametrów fizycznych,
        /// a następnie wybiera odpowiednie jednostki i format wyświetlania, aby zapewnić czytelność
        /// dla szerokiego zakresu wartości. Aktualizuje pola tekstowe z wynikami.
        /// </remarks>
        private void CalculateTrajectoryParameters()
        {
            double angleRadians = angle * Math.PI / 180;
            double planetGravity = currentPlanet.Gravity;

            // Składowe prędkości początkowej
            double v0x = initialVelocity * Math.Cos(angleRadians);
            double v0y = initialVelocity * Math.Sin(angleRadians);

            // Czas lotu
            double flightTime = (v0y + Math.Sqrt(v0y * v0y + 2 * planetGravity * initialHeight)) / planetGravity;

            // Maksymalna wysokość
            double maxHeight = initialHeight + v0y * v0y / (2 * planetGravity);

            // Zasięg rzutu
            double range = v0x * flightTime;

            // Wybór jednostek w zależności od wartości
            string heightUnit = "m", rangeUnit = "m";
            double heightValue = maxHeight, rangeValue = range;

            //Dostosowanie jednostek wysokości i formatowanie
            string heightFormat = "F2"; // Domyślnie 2 miejsca dziesiętne
            if (maxHeight >= 1000000)
            {
                heightUnit = "Mm"; //Megametr - i tak nie zadziala bo nie rysuje megametrow na skali ale wykona
                heightValue = maxHeight / 1000000;   // sie dla km
                heightFormat = "F1"; //1 miejsce dziesiętne dla bardzo dużych wartości
            }
            else if (maxHeight >= 1000)
            {
                heightUnit = "km";
                heightValue = maxHeight / 1000;
                heightFormat = "F1";
            }
            else if (maxHeight < 0.1 && maxHeight >= 0.001)
            {
                heightUnit = "cm";
                heightValue = maxHeight * 100;
            }
            else if (maxHeight < 0.001)
            {
                heightUnit = "mm";
                heightValue = maxHeight * 1000;
                heightFormat = "F1";
            }

            //Dostosowanie jednostek zasięgu i formatowanie
            string rangeFormat = "F2";
            if (range >= 1000000)
            {
                rangeUnit = "Mm";
                rangeValue = range / 1000000;
                rangeFormat = "F1";
            }
            else if (range >= 1000)
            {
                rangeUnit = "km";
                rangeValue = range / 1000;
                rangeFormat = "F1";
            }
            else if (range < 0.1 && range >= 0.001)
            {
                rangeUnit = "cm";
                rangeValue = range * 100;
            }
            else if (range < 0.001)
            {
                rangeUnit = "mm";
                rangeValue = range * 1000;
                rangeFormat = "F1";
            }

            //Dostosowanie jednostek czasu i formatowanie
            string timeUnit = "s";
            double timeValue = flightTime;
            string timeFormat = "F2";
            if (flightTime >= 3600)
            {
                timeUnit = "h";
                timeValue = flightTime / 3600;
                timeFormat = "F1";
            }
            else if (flightTime >= 60)
            {
                timeUnit = "min";
                timeValue = flightTime / 60;
                timeFormat = "F1";
            }
            else if (flightTime < 0.1 && flightTime >= 0.001)
            {
                timeUnit = "ms";
                timeValue = flightTime * 1000;
            }
            else if (flightTime < 0.001)
            {
                timeUnit = "μs";
                timeValue = flightTime * 1000000;
                timeFormat = "F1";
            }

            //Aktualizacja pól informacyjnych z uwzględnieniem jednostek i formatowania
            MaxHeightText.Text = $"{heightValue.ToString(heightFormat)} {heightUnit}";
            RangeText.Text = $"{rangeValue.ToString(rangeFormat)} {rangeUnit}";
            FlightTimeText.Text = $"{timeValue.ToString(timeFormat)} {timeUnit}";
        }
    }
}