using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ParticlePlay
{
    class DrawingVisualElement : FrameworkElement
    {
        public DrawingVisual visual;

        public DrawingVisualElement() { visual = new DrawingVisual(); }

        protected override int VisualChildrenCount { get { return 1; } }

        protected override Visual GetVisualChild(int index) { return visual; }
    }

    public class Particle
    {
        public Point Position;
        public Point Velocity;
        public Color Color;
        public double Lifespan;
        public double Elapsed;

        public void Update(double elapsedSeconds)
        {
            Elapsed += elapsedSeconds;
            if (Elapsed > Lifespan)
            {
                Color.A = 0;
                return;
            }
            Color.A = (byte)(255 - ((255 * Elapsed)) / Lifespan);
            Position.X += Velocity.X * elapsedSeconds;
            Position.Y += Velocity.Y * elapsedSeconds;
        }
    }

    public class ParticleEmitter
    {
        public Point Center { get; set; }
        public List<Particle> particles = new List<Particle>();
        Random rand = new Random();
        public WriteableBitmap TargetBitmap;
        public WriteableBitmap ParticleBitmap;

        void CreateParticle()
        {
            var speed = rand.Next(20) + 140;
            var angle = (2 * Math.PI * rand.NextDouble());
            
            //triangle
            //var angle = (2 * Math.PI * (rand.NextDouble() / 8)) + 1.1;

            // straight down
            //var angle = 6.25;

            //straight up
            // var angle = 3.1;

            particles.Add(
                new Particle()
                {
                    Position = new Point(Center.X, Center.Y),
                    Velocity = new Point(
                        Math.Sin(angle) * speed,
                        Math.Cos(angle) * speed),
                    Color = Color.FromRgb(0, 255, 0),
                    Lifespan = 0.5 + rand.Next(200) / 1000d
                });
        }

        public void Update()
        {
            var updateInterval = .003;

            CreateParticle();

            particles.RemoveAll(p =>
            {
                p.Update(updateInterval);
                return p.Color.A == 0;
            });
        }
    }

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var canvas = new Canvas();

            Content = canvas;

            var emitter = new ParticleEmitter();

            var element = new DrawingVisualElement();

            Random rnd = new Random();

            CompositionTarget.Rendering += (s, e) =>
            {
                emitter.Update();

                canvas.Children.Clear();

                canvas.Children.Add(new Rectangle() { Fill = Brushes.Black, Width = canvas.ActualWidth, Height = canvas.ActualHeight });

                using (var dc = element.visual.RenderOpen())
                {
                    //dc.DrawRectangle(
                    //    Brushes.Black,
                    //    null,
                    //    new Rect(
                    //        new Size(
                    //            canvas.ActualWidth,
                    //            canvas.ActualHeight)));

                    emitter.particles.ForEach(p =>
                    {

                        var c = p.Color;

                        c.A /= 2;

                        //dc.DrawLine(new Pen(new SolidColorBrush(c),1),  p.Position, new Point(p.Position.X , p.Position.Y + rnd.Next(-100, 100) )
                        //        );
                    
                        dc.DrawEllipse(
                           new SolidColorBrush(c),
                            null, p.Position, 7, 7);

                        dc.DrawEllipse(
                            new SolidColorBrush(p.Color),
                            null, p.Position, 2, 2);
                    });
                }

                canvas.Children.Add(element);
            };

              MouseMove += (s, e) => emitter.Center = e.GetPosition(canvas);
            
        }

        
    }
}
