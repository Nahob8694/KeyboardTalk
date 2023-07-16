using System.Reflection;

namespace KeydownEventService
{
    public class KeydownEventHandler
    {
        class EventEmitter
        {
            public class EventExecutor
            {
                public event Action? Actions;

                public void Execute()
                {
                    Actions?.Invoke();
                }
            }

            private readonly Dictionary<Keys, EventExecutor> _events;

            public EventEmitter()
            {
                _events = new Dictionary<Keys, EventExecutor>();
            }

            public void On(Keys key, Action action)
            {
                _events.TryGetValue(key, out EventExecutor? eventExecutor);
                if (eventExecutor is null)
                {
                    eventExecutor = new EventExecutor();
                    eventExecutor.Actions += action;
                    _events[key] = eventExecutor;
                }
                else
                {
                    eventExecutor.Actions += action;
                }
            }

            public void Emit(Keys key)
            {
                _events.TryGetValue(key, out EventExecutor? eventExecutor);
                eventExecutor?.Execute();
            }
        }

        public event Action<Keys, Exception>? OnError;
        public event Action<Keys>? OnPressed;
        private readonly List<Type> _modules;
        private readonly EventEmitter _emitter;
        public KeydownEventHandler() 
        {
            _modules = new List<Type>();
            _emitter = new EventEmitter();
        }

        public void Load(Type type)
        {
            if(!type.IsSubclassOf(typeof(KeydownEventBase)))
            {
                throw new ArgumentException("Type must be a subclass of KeydownEventBase.");
            }

            if (_modules.Contains(type)) return;

            object? instance = Activator.CreateInstance(type);
            MethodInfo[] methods = type.GetMethods();

            foreach(MethodInfo method in methods)
            {
                KeyAttribute? keyAttribute = method.GetCustomAttribute<KeyAttribute>();
                if(keyAttribute is not null)
                {
                    Keys[] keys = keyAttribute.Keys;
                    Action action = (Action)Delegate.CreateDelegate(typeof(Action), instance, method);
                    foreach(Keys key in keys)
                    {
                        _emitter.On(key, action);
                    }
                }
            }

            _modules.Add(type);
        }

        public void LoadFromAssembly(Assembly assembly)
        {
            Type[] types = assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(KeydownEventBase)) && x.GetCustomAttribute<DontAutoRegisterAttribute>() is null).ToArray();

            foreach(Type type in types)
            {
                Load(type);
            }
        }

        public void Press(Keys key)
        {
            try
            {
                _emitter.Emit(key);
                OnPressed?.Invoke(key);
            }
            catch (Exception e)
            {
                OnError?.Invoke(key, e);
            }
        }

        public void RemoveAllListener()
        {
            OnPressed -= OnPressed;
            OnError -= OnError;
        }
    }
}
