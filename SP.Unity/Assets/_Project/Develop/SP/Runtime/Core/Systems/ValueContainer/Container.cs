using System.Collections.Generic;

namespace SP.Runtime.Core.Systems.ValueContainer
{
    public abstract class Container<T>
    {
        public class Element
        {
            public Element(T value, bool modify = false)
            {
                Value = value;
                
                _modify = modify;
            }

            public T Value { get; private set; }

            private readonly bool _modify;

            public void ModifyValue(T value)
            {
                if (_modify)
                {
                    Value = value;
                }
            }
        }
    
        protected readonly List<Element> total = new();

        public abstract T GetTotal();

        public IReadOnlyList<T> GetUniqueEnumerable()
        {
            List<T> output = new();

            foreach (var e in total)
            {
                if (!output.Contains(e.Value))
                {
                    output.Add(e.Value);
                }
            }

            return output;
        }
        
        public Element Add(T value)
        {
            var output = new Element(value);
            
            total.Add(output);
        
            return output;
        }
        
        public void Remove(Element value)
        {
            if (!total.Contains(value))
            {
                return;
            }
        
            total.Remove(value);
        }
    }
}