using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pairs
{
    public class MiPar<T,R>
    {
        #region Campos
        T key;
        R value;
        #endregion

        #region Constructores
        public MiPar(T key, R value)
        {
            this.key = key;
            this.value = value;
        } 
        #endregion

        #region Propiedades
        public T Key
        {
            get { return key; }
            set { key = value; }
        }

        public R Value
        {
            get { return value; }
            set { this.value = value; }
        } 
        #endregion

    }
}
