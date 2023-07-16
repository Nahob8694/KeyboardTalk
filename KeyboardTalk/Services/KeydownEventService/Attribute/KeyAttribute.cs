namespace KeydownEventService
{
    [AttributeUsage(AttributeTargets.Method)]
    public class KeyAttribute : Attribute
    {
        public Keys[] Keys;

        public KeyAttribute(params Keys[] keys)
        {
            Keys = keys;
        }
    }
}
