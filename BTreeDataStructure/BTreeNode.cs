using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTreeDataStructure
{
    /// <summary>
    /// Representa un nodo de B-Tree
    /// </summary>
    /// <typeparam name="T">Tipo de la llave</typeparam>
    /// <typeparam name="R">Tipo del valor</typeparam>
    public class BTreeNode<T,R>
    {
        #region Campos
        /// <summary>
        /// Branching Factor
        /// </summary>
        int t;
        
        /// <summary>
        /// Indica si el nodo es hoja
        /// </summary>
        bool isLeaf;
        
        /// <summary>
        ///  Indica la cantidad de llaves que tiene el nodo. 
        /// </summary>
        int cantKeys;
        
        /// <summary>
        /// Llaves del nodo de BTree
        /// </summary>
        T[] keys;

        /// <summary>
        ///  Informacion satellite de las llaves.
        /// </summary>
        R[] satellite;
        
        /// <summary>
        /// Referencia en el archivo a los nodos hijos de este nodo.
        /// </summary>
        long[] children;
        
        /// <summary>
        /// Posicion o referencia en el archivo de este nodo.
        /// </summary>
        long posicionArchivo;

        #endregion

        #region Constructores
        /// <summary>
        /// Constructor vacío para casos auxiliares
        /// </summary>
        public BTreeNode()
        {

        }
        /// <summary>
        /// Crea un nodo de B-Tree
        /// </summary>
        /// <param name="posicionArchivo">Posición del nodo en disco duro</param>
        /// <param name="t">t del B-Tree</param>
        public BTreeNode(long posicionArchivo, int t)
        {
            this.t = t;
            this.posicionArchivo = posicionArchivo;

            keys = new T[2 * t - 1];
            satellite = new R[2 * t - 1];
            children = new long[2 * t];

            isLeaf = true;
            cantKeys = 0;
        } 
        #endregion

        #region Properties
        internal bool IsLeaf
        {
            get { return isLeaf; }
            set { isLeaf = value; }
        } 
        internal int CantKeys
        {
            get { return cantKeys; }
            set 
            {
                if (value < 0||value > keys.Length)
                   throw new ArgumentException("La cantidad de llaves no puede se negativa");
                cantKeys = value; 
            }
        } 
        internal T[] Keys
        {
            get { return keys; }
        }
        internal long[] Children
        {
            get { return children; }
        }
        internal R[] Satellite
        {
            get { return satellite; }
        }
        internal long PosFile
        {
            get { return posicionArchivo; }
            set { posicionArchivo = value; }
        }
        internal bool IsFull
        {
            get { return cantKeys == 2 * t - 1; }
        }
        #endregion
    }
}
