namespace System.Windows.Forms
{
    public class KeyboardButton : Button
    {
        public event Action<object, Keys>? Pressed;
        public Keys Key { get; set; }

        public KeyboardButton(): base()
        {
            Width = 48;
            Height = 48;
            Font = new Font(Font.FontFamily, 7, FontStyle.Regular);
            Click += KeyPressedEvent;
        }

        public virtual void OnPressed()
        {
            Pressed?.Invoke(this, Key);
        }

        private void KeyPressedEvent(object? sender, EventArgs e)
        {
            OnPressed();
        }
    }
}
