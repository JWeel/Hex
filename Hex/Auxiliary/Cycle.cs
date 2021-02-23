namespace Hex.Auxiliary
{
    public class Cycle<T>
    {
        #region Constructors

        public Cycle(params T[] values)
        {
            this.Index = 0;
            this.Values = values;
        }

        #endregion

        #region Properties

        protected T[] Values { get; set; }
        
        public int Index { get; protected set; }

        public T Value =>
            this.Values[this.Index];

        #endregion

        #region Methods

        public void Advance()
        {
            var index = this.Index + 1;
            if (index >= this.Values.Length)
                index = 0;
            this.Index = index;
        }

        public void Reverse()
        {
            var index = this.Index - 1;
            if (index < 0)
                index = this.Values.Length - 1;
            this.Index = index;
        }

        public void Reset() =>
            this.Index = 0;
            
        public static implicit operator T (Cycle<T> cycle) =>
            cycle.Value;

        #endregion
    }
}