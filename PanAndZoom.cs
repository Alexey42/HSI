using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HSI
{
    public class ZoomBorder : Border
    {
        private UIElement child = null;
        private Point origin;
        private Point start;
        public bool isActive = true;

        private TranslateTransform GetTranslateTransform(UIElement element)
        {
            return (TranslateTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is TranslateTransform);
        }

        private ScaleTransform GetScaleTransform(UIElement element)
        {
            return (ScaleTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is ScaleTransform);
        }

        public override UIElement Child
        {
            get { return base.Child; }
            set
            {
                if (value != null && value != this.Child)
                    this.Initialize(value);
                base.Child = value;
            }
        }

        public void Initialize(UIElement element)
        {
            this.child = element;
            if (child != null)
            {
                TransformGroup group = child.RenderTransform as TransformGroup;
                if (group == null || !group.Children.Any(tr => tr is ScaleTransform) || !group.Children.Any(tr => tr is TranslateTransform))
                {
                    group = new TransformGroup();
                    group.Children.Add(new ScaleTransform());
                    group.Children.Add(new TranslateTransform());
                    child.RenderTransform = group;
                }
                child.RenderTransformOrigin = new Point(0.0, 0.0);
                this.MouseWheel += child_MouseWheel;
                this.MouseLeftButtonDown += child_MouseLeftButtonDown;
                this.MouseLeftButtonUp += child_MouseLeftButtonUp;
                this.MouseMove += child_MouseMove;
                this.PreviewMouseRightButtonDown += new MouseButtonEventHandler(
                  child_PreviewMouseRightButtonDown);
            }
        }

        public void Reset()
        {
            if (child != null)
            {
                // reset zoom
                var st = GetScaleTransform(child);
                st.ScaleX = 1.0;
                st.ScaleY = 1.0;

                // reset pan
                var tt = GetTranslateTransform(child);
                tt.X = 0.0;
                tt.Y = 0.0;
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (child != null)
                child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            double width = double.IsInfinity(constraint.Width) && child != null ? child.DesiredSize.Width : constraint.Width;
            double height = double.IsInfinity(constraint.Height) && child != null ? child.DesiredSize.Height : constraint.Height;
            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            if (child != null)
                child.Arrange(new Rect(new Point(0, 0), child.DesiredSize));

            return arrangeSize;
        }

        public void FitToBounds(double contentWidth, double contentHeight)
        {
            double viewportWidth = ActualWidth;
            double viewportHeight = ActualHeight;

            FrameworkElement parent = Parent as FrameworkElement;
            if ((viewportWidth <= 0 || viewportHeight <= 0) && parent != null)
            {
                viewportWidth = parent.ActualWidth;
                viewportHeight = parent.ActualHeight;
            }

            if (child == null || contentWidth <= 0 || contentHeight <= 0 || viewportWidth <= 0 || viewportHeight <= 0)
                return;

            var st = GetScaleTransform(child);
            var tt = GetTranslateTransform(child);
            double scale = System.Math.Min(viewportWidth / contentWidth, viewportHeight / contentHeight);
            scale = System.Math.Min(1.0, scale);
            if (scale <= 0)
                scale = 1.0;

            st.ScaleX = scale;
            st.ScaleY = scale;
            tt.X = (viewportWidth - contentWidth * scale) / 2.0;
            tt.Y = (viewportHeight - contentHeight * scale) / 2.0;
        }

        #region Child Events

        private void child_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (child == null || !isActive) return;
            
            var st = GetScaleTransform(child);
            var tt = GetTranslateTransform(child);

            double zoom = e.Delta > 0 ? .3 : -.3;
            if (!(e.Delta > 0) && (st.ScaleX < .1 || st.ScaleY < .1))
                return;

            Point relative = e.GetPosition(child);
            double absoluteX;
            double absoluteY;

            absoluteX = relative.X * st.ScaleX + tt.X;
            absoluteY = relative.Y * st.ScaleY + tt.Y;

            st.ScaleX += zoom;
            st.ScaleY += zoom;

            tt.X = absoluteX - relative.X * st.ScaleX;
            tt.Y = absoluteY - relative.Y * st.ScaleY;
            
        }

        private void child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (child == null || !isActive) return;
            
            var tt = GetTranslateTransform(child);
            start = e.GetPosition(this);
            origin = new Point(tt.X, tt.Y);
            child.CaptureMouse();
            
        }

        private void child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (child == null || !isActive) return;    
            child.ReleaseMouseCapture();
            this.Cursor = Cursors.Arrow;  
        }

        void child_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Reset();
        }

        private void child_MouseMove(object sender, MouseEventArgs e)
        {
            if (child == null || !isActive) return;

            if (child.IsMouseCaptured)
            {
                var tt = GetTranslateTransform(child); 
                Vector v = start - e.GetPosition(this);
                tt.X = origin.X - v.X;
                tt.Y = origin.Y - v.Y;
            }
        }

        #endregion
    }
}
