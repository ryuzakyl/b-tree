using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using Pairs;
using FileHandler;

namespace BTreeDataStructure
{
    /// <summary>
    /// Estructura de Datos B-Tree
    /// </summary>
    /// <typeparam name="T">Llave de tipo genérico T</typeparam>
    /// <typeparam name="R">Valor de tipo genérico R</typeparam>
    public class BTree<T, R>
        where T : IComparable, IGuardable, new()
        where R : IComparable, IGuardable, new()
    {


        #region Campos
        BTreeNode<T, R> root;//raiz que siempre debe estar en RAM
        private static int t;


        private static long nodeSize;
        private static int keySize;   //coincide con T.Size
        private static int valueSize; //coincide con R.Size

        CacheMemory<T, R> RAM;
        static FileManager fm;

        #endregion

        #region Constructores
        /// <summary>
        ///  Constructor para crear un nuevo B-Tree
        /// </summary>
        /// <param name="branchingFactor">t</param>
        /// <param name="handler">FileStream para leer y escribir en disco</param>
        /// <param name="sizeOfKey">Tamaño de la llave</param>
        /// <param name="sizeOfValue">Tamaño del valor</param>
        public BTree(int branchingFactor, FileManager handler, int sizeOfKey, int sizeOfValue)
        {
            if (branchingFactor < 2)
                throw new ArgumentOutOfRangeException("El branching factor debe ser mayor o igual que 2.");
            t = branchingFactor;

            keySize = sizeOfKey;
            valueSize = sizeOfValue;

            RAM = new CacheMemory<T, R>(50);
            fm = handler;

           
            CalculateNodeSize();

            BTreeCreate();
        }
        /// <summary>
        /// Constructor para levantar la raíz de un B-Tree ya creado
        /// </summary>
        /// <param name="rootPointer">Puntero a la raíz del B-Tree</param>
        /// <param name="branchingFactor">t</param>
        /// <param name="handler">FileStream para leer y escribir en disco</param>
        /// <param name="sizeOfKey">Tamaño de la llave</param>
        /// <param name="sizeOfValue">Tamaño del valor</param>
        public BTree(long rootPointer, int branchingFactor, FileManager handler, int sizeOfKey, int sizeOfValue)
        {
            if (branchingFactor < 2)
                throw new ArgumentOutOfRangeException("El branching factor debe ser mayor o igual que 2.");
            t = branchingFactor;

            
            keySize = sizeOfKey;
            valueSize = sizeOfValue;
            
            RAM = new CacheMemory<T, R>(50);
            fm = handler;
            
            CalculateNodeSize();
            
            root = DISK_READ(rootPointer);
        }
        #endregion

        #region Búsquedas
        /// <summary>
        /// Busca un par {llave,valor} en el B-Tree.
        /// </summary>
        /// <param name="key">Llave a buscar</param>
        /// <param name="value">Valor a buscar</param>
        /// <returns></returns>
        public MiPar<T, R> Search(T key, R value)
        {
            if (key == null || value == null)
                throw new InvalidOperationException("No es válida la búsqueda de un NULL");
            return Search(root, key, value);
        }
        /// <summary>
        ///  Devuelve un par indicando si el par {key,value} pertenece al subárbol con la
        ///  raíz x.
        /// </summary>
        /// <param name="x">Raíz del subárbol</param>
        /// <param name="key">Llave abuscar</param>
        /// <param name="value">Valor a Buscar</param>
        /// <returns></returns>
        MiPar<T, R> Search(BTreeNode<T, R> x, T key, R value)
        {
            int i = 0;
            while (i < x.CantKeys && Compare(key, x.Keys[i], value, x.Satellite[i]) > 0)
            {
                i++;
            }
            if (i < x.CantKeys && Compare(key, x.Keys[i], value, x.Satellite[i]) == 0)
                return new MiPar<T, R>(x.Keys[i], x.Satellite[i]);
            if (x.IsLeaf)
                return null;

            BTreeNode<T, R> Ci = GetNode(x.Children[i]);
            return Search(Ci, key, value);
        }
        /// <summary>
        /// Indica si el par {llave,valor} está contenida en el B-Tree
        /// </summary>
        /// <param name="key">Llave a verificar</param>
        /// <returns></returns>
        public bool Contains(T key, R value)
        {
            MiPar<T, R> result = Search(key, value);
            if (result == null)
                return false;
            return true;
        }
        /// <summary>
        /// Devuelve los elementos en el intervalo( {keyMin,min} ; {keyMax,max} )
        /// </summary>
        /// <param name="keyMin"></param>
        /// <param name="keyMax"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<T, R>> SearchBetween(T keyMin, T keyMax, R min, R max)
        {
            foreach (var item in SearchBetween(root, keyMin, keyMax, min, max))
                yield return item;
        }
        /// <summary>
        ///  Devuelve los elementos en el intervalo( {keyMin,min} ; {keyMax,max} )
        /// </summary>
        /// <param name="x">Nodo actual</param>
        /// <param name="keyMin">Llave minima</param>
        /// <param name="keyMax">Llave máxima</param>
        /// <param name="min">Valor Mínimo</param>
        /// <param name="max">Valor Máximo</param>
        /// <returns></returns>
        IEnumerable<KeyValuePair<T, R>> SearchBetween(BTreeNode<T, R> x, T keyMin, T keyMax, R min, R max)
        {
            int i = buscarEnNodo(x, keyMin, min, true);
            if (x.IsLeaf)
            {
                while (i < x.CantKeys && Compare(x.Keys[i], keyMax, x.Satellite[i], max) < 0)// <0 porque se excluyen los bordes
                {
                    yield return new KeyValuePair<T, R>(x.Keys[i], x.Satellite[i]);
                    i++;
                }
            }
            else
            {
                if (i < x.CantKeys)
                {
                    if (Compare(x.Keys[i], keyMax, x.Satellite[i], max) > 0)//si mayor que el maximo
                    {
                        BTreeNode<T, R> node = GetNode(x.Children[i]);
                        foreach (var item in SearchBetween(node, keyMin, keyMax, min, max))
                            yield return item;
                    }
                    while (i < x.CantKeys && Compare(x.Keys[i], keyMax, x.Satellite[i], max) < 0)// <0 porque se excluyen los bordes 
                    {
                        BTreeNode<T, R> node = GetNode(x.Children[i]);
                        foreach (var item in SearchBetween(node, keyMin, keyMax, min, max))
                            yield return item;
                        yield return new KeyValuePair<T, R>(x.Keys[i], x.Satellite[i]);
                        i++;
                    }
                }
                else
                {
                    BTreeNode<T, R> node = GetNode(x.Children[i]);
                    foreach (var item in SearchBetween(node, keyMin, keyMax, min, max))
                        yield return item;
                }
            }
        } 
        #endregion

        #region Insert
        /// <summary>
        ///  Inserta el par {llave,valor} en el B-Tree
        /// </summary>
        /// <param name="k">Llave a Insertar</param>
        /// <param name="value">Valor a Insertar</param>
        public void Insert(T k, R value)
        {
            BTreeNode<T, R> r = root;
            if (root.IsFull)
            {
                BTreeNode<T, R> s = AllocateNode();
                root = s;
                s.CantKeys = 0;
                s.IsLeaf = false;
                s.Children[0] = r.PosFile;
                
                if (RAM.Contains(r.PosFile) == null)//Para que cuando la raiz sea nueva y se hagan
                    RAM.Add(r);//cambios a la raiz anterior se escriban en disco

                SplitChild(s, 0, r);
                InsertNonFull(s, k, value);
            }
            else
                InsertNonFull(root, k, value);
        }
        /// <summary>
        ///  Inserta el par {llave,valor} en un nodo no lleno
        /// </summary>
        /// <param name="x">Llave a Insertar</param>
        /// <param name="k">Valor a Insertar</param>
        /// <param name="value"></param>
        void InsertNonFull(BTreeNode<T, R> x, T k, R value)
        {
            int index = buscarEnNodo(x, k, value, true);
            if (x.IsLeaf)
            {
                if (x.Keys[index] != null && Compare(k, x.Keys[index], value, x.Satellite[index]) == 0)
                    return;
                Array.Copy(x.Keys, index, x.Keys, index + 1, x.CantKeys - index);
                Array.Copy(x.Satellite, index, x.Satellite, index + 1, x.CantKeys - index);

                x.Keys[index] = k;
                x.Satellite[index] = value;
                x.CantKeys++;
            }
            else
            {
                //Aqui va la insercion de la llave si x no es hoja

                if (x.Keys[index] != null && Compare(k, x.Keys[index], value, x.Satellite[index]) == 0)
                    return;
                
                BTreeNode<T, R> CiX = GetNode(x.Children[index]); 
                bool inc = false;
                if (CiX.IsFull)
                {
                    SplitChild(x, index, CiX);
                    if (Compare(k, x.Keys[index], value, x.Satellite[index]) > 0)
                    {
                        index++;
                        inc = true;
                    }
                }
                CiX = inc ? GetNode(x.Children[index]) : CiX;
                InsertNonFull(CiX, k, value);
            }
        }
        /// <summary>
        /// Realiza la operacion de dividir un nodo
        /// </summary>
        /// <param name="x">Nodo interno no lleno(debe estar cargado en memoria)</param>
        /// <param name="i">Un indice i</param>
        /// <param name="y">Un nodo y tal que y=Ci[x] es un nodo lleno(debe estar cargado en memoria)</param>
        void SplitChild(BTreeNode<T, R> x, int i, BTreeNode<T, R> y)
        {
            BTreeNode<T, R> z = AllocateNode();
            z.IsLeaf = y.IsLeaf;
            z.CantKeys = t - 1;
            for (int j = 0; j < t - 1; j++)//copia de las 2das t-1 llaves de y
            {
                z.Keys[j] = y.Keys[t + j];//copia de las llaves
                z.Satellite[j] = y.Satellite[t + j];//added
            }
            if (!y.IsLeaf)
            {
                for (int j = 0; j <= t - 1; j++)//added =
                    z.Children[j] = y.Children[t + j];//copia de los hijos
            }
            y.CantKeys = t - 1;

            for (int j = x.CantKeys; j > i; j--)//corrimiento de los hijos de x
                x.Children[j + 1] = x.Children[j];
            x.Children[i + 1] = z.PosFile;
            for (int j = x.CantKeys - 1; j >= i; j--)//corrimiento de las llaves de x
            {
                x.Keys[j + 1] = x.Keys[j];
                x.Satellite[j + 1] = x.Satellite[j];//added
            }
            x.Keys[i] = y.Keys[t - 1];
            x.Satellite[i] = y.Satellite[t - 1];//added
            x.CantKeys++;
        }
        #endregion
        
        #region Creación del B-Tree y sus nodos
        /// <summary>
        /// Se crea un nuevo nodo y se devuelve
        /// </summary>
        /// <returns></returns>
        private BTreeNode<T, R> AllocateNode()
        {
            long freeMemory = fm.Alloc();
            BTreeNode<T, R> result = new BTreeNode<T, R>(freeMemory, t);

            //Se agrega el nodo a la cache.
            RAM.Add(result);

            return result;
        }

        /// <summary>
        /// Crea un B-Tree
        /// </summary>
        private void BTreeCreate()
        {
            BTreeNode<T, R> x = AllocateNode();
            x.IsLeaf = true;
            x.CantKeys = 0;
            RAM.Update(x);
            root = x;
        } 
        #endregion

        #region Delete
        /// <summary>
        ///  Elimina del B-Tree el par {llave,valor}
        /// </summary>
        /// <param name="k">Llave a eliminar</param>
        /// <param name="v">Valor a eliminar</param>
        /// <returns></returns>
        public MiPar<T, R> Delete(T k, R v)
        {
            return Delete(root, k, v);
        }
        /// <summary>
        /// Elimina del B-Tree el par {llave,valor} a partir de la raíz
        /// </summary>
        /// <param name="x">Nodo raíz</param>
        /// <param name="k">Llave a eliminar</param>
        /// <param name="v">Valor a eliminar</param>
        /// <returns></returns>
        private MiPar<T, R> Delete(BTreeNode<T, R> x, T k, R v)
        {
            int index = buscarEnNodo(x, k, v, false);
            //int index = buscarEnNodoSec(x, k, v);
            if (x.IsLeaf)//Caso 1
            {
                if (index == -1)
                    return null;
                MiPar<T, R> result = new MiPar<T, R>(x.Keys[index], x.Satellite[index]);
                for (int i = index; i < x.CantKeys - 1; i++)
                {
                    x.Keys[i] = x.Keys[i + 1];
                    x.Satellite[i] = x.Satellite[i + 1];
                }
                x.CantKeys--;
                //RAM.Update(x);
                return result;
            }
            else //x es un nodo interno(casos 2 y 3)
            {
                if (index != -1)//x contiene a k(caso 2)
                {
                    BTreeNode<T, R> y = GetNode(x.Children[index]);//llamar al FileManager y DISK_READ(x.Children[index]) predecessor child;
                    if (y.CantKeys >= t)//y tiene al menos t llaves
                    {//caso 2a
                        BTreeNode<T, R> max = Maximo(y);
                        T aux = max.Keys[max.CantKeys - 1];
                        R val = max.Satellite[max.CantKeys - 1];//addded
                        x.Keys[index] = aux;
                        x.Satellite[index] = val;//added
                        //RAM.Update(x);//added
                        return Delete(y, aux, val);
                    }
                    else //y tiene t-1 llaves 
                    { //caso 2b
                        BTreeNode<T, R> z = GetNode(x.Children[index + 1]);//llamar al FileManager y DISK_READ(x.Children[index + 1]) sucessor child;
                        if (z.CantKeys >= t)
                        {
                            BTreeNode<T, R> min = Minimo(z);
                            T aux = min.Keys[0];
                            R val = min.Satellite[0];//added
                            x.Keys[index] = aux;
                            x.Satellite[index] = val;//added
                            //RAM.Update(z);//added
                            return Delete(z, aux, val);
                        }
                        else//tanto y como z tienen t-1 llaves 
                        {//caso 2c
                            Merge(x, y, z, index);//revisar bien aqui(buscar en la hoja del pseudocodigo)
                            return Delete(y, k, v);
                        }
                    }
                }
                else//x no contiene a k(caso 3)
                {
                    int i = 0;
                    while (i < x.CantKeys && Compare(k, x.Keys[i], v, x.Satellite[i]) > 0)//change k.CompareTo(x.Keys[i]) > 0
                        i++;
                    BTreeNode<T, R> c = GetNode(x.Children[i]);//DISK_READ(x.Children[i]);

                    if (c.CantKeys == t - 1)
                    {
                        bool primero = (i == 0);
                        bool ultimo = (i == x.CantKeys);
                        BTreeNode<T, R> y = new BTreeNode<T, R>();
                        BTreeNode<T, R> z = new BTreeNode<T, R>();

                        if (!primero)
                            y = GetNode(x.Children[i - 1]);// DISK_READ(x.Children[i-1]) predecessor brother;
                        if (!ultimo)
                            z = GetNode(x.Children[i + 1]);//DISK_READ(x.Children[i+1]) sucessor brother;
                        if ((!primero && y.CantKeys >= t) || (!ultimo && z.CantKeys >= t))//caso 3a
                        {
                            if (!primero && y.CantKeys >= t)
                            {
                                //la llave a bajar del padre es la i-1
                                Array.Copy(c.Keys, 0, c.Keys, 1, c.CantKeys);
                                Array.Copy(c.Satellite, 0, c.Satellite, 1, c.CantKeys);//added
                                Array.Copy(c.Children, 0, c.Children, 1, c.CantKeys + 1);
                                c.Keys[0] = x.Keys[i - 1];
                                c.Satellite[0] = x.Satellite[i - 1];//added
                                c.Children[0] = y.Children[y.CantKeys];
                                x.Keys[i - 1] = y.Keys[y.CantKeys - 1];
                                x.Satellite[i - 1] = y.Satellite[y.CantKeys - 1];//added

                                c.CantKeys++;
                                y.CantKeys--;

                                //RAM.Update(x);
                                //RAM.Update(c);
                                //RAM.Update(y);
                            }
                            else if (!ultimo && z.CantKeys >= t)
                            {
                                //la llave a bajar del padre es la i
                                c.Keys[c.CantKeys] = x.Keys[i];
                                c.Satellite[c.CantKeys] = x.Satellite[i];//added
                                c.Children[c.CantKeys + 1] = z.Children[0];
                                x.Keys[i] = z.Keys[0];
                                x.Satellite[i] = z.Satellite[0];//added
                                Array.Copy(z.Keys, 1, z.Keys, 0, z.CantKeys - 1);
                                Array.Copy(z.Satellite, 1, z.Satellite, 0, z.CantKeys - 1);//added
                                Array.Copy(z.Children, 1, z.Children, 0, z.CantKeys);
                                c.CantKeys++;
                                z.CantKeys--;

                                //RAM.Update(x);
                                //RAM.Update(c);
                                //RAM.Update(z);
                            }
                            return Delete(c, k, v);
                        }
                        else //c y sus dos hermanos tienen t-1 llaves
                        {
                            //caso 3b
                            if (!primero)
                            {
                                Merge(x, y, c, i - 1);
                                return Delete(y, k, v);
                            }
                            else /*if (!ultimo)*/
                            {
                                Merge(x, c, z, i/*i-1*/);
                                return Delete(c, k, v);
                            }
                        }
                        // if (!primero && y.CantKeys >= t)
                        // return Delete(y, k, v);
                        // else 
                        // return Delete(c, k, v);
                    }
                    else return Delete(c, k, v);
                }
            }
        }

        /// <summary>
        /// Mezcla 2 nodos de t-1 llaves cada uno.
        /// </summary>
        /// <param name="parent">Nodo padre</param>
        /// <param name="leftChild">Hijo izquierdo</param>
        /// <param name="rightChild">Hijo derecho</param>
        /// <param name="keyIndex">Indice de la llave que los va a separar</param>
        private void Merge(BTreeNode<T, R> parent, BTreeNode<T, R> leftChild, BTreeNode<T, R> rightChild, int keyIndex)
        {
            leftChild.Keys[t - 1] = parent.Keys[keyIndex];
            leftChild.Satellite[t - 1] = parent.Satellite[keyIndex];//added

            Array.Copy(rightChild.Keys, 0, leftChild.Keys, t, t - 1);//copia de las llaves
            Array.Copy(rightChild.Satellite, 0, leftChild.Satellite, t, t - 1);//added(copia de los valores)
            Array.Copy(rightChild.Children, 0, leftChild.Children, t, t);//copia de los hijos

            leftChild.CantKeys = 2 * t - 1;

            for (int i = keyIndex + 1; i < parent.CantKeys; i++)
            {
                parent.Keys[i - 1] = parent.Keys[i];
                parent.Satellite[i - 1] = parent.Satellite[i];//added
                parent.Children[i] = parent.Children[i + 1];
            }
          
            parent.CantKeys--;

            if (parent.CantKeys == 0)
            {
                long toFree = root.PosFile;
                root = leftChild;
                fm.Free(toFree);
                RAM.Remove(parent);
            }
           
            fm.Free(rightChild.PosFile);//Siempre se libera el nodo rightChild
            RAM.Remove(rightChild);
        }

        /// <summary>
        /// Busca el mayor elemento en el subárbol de raíz x.
        /// </summary>
        /// <param name="x">Raíz del subárbol</param>
        /// <returns></returns>
        private BTreeNode<T, R> Maximo(BTreeNode<T, R> x)
        {
            BTreeNode<T, R> maximo = x;
            while (!maximo.IsLeaf)
                maximo = GetNode(maximo.Children[maximo.CantKeys]);
            return maximo;
        }
        /// <summary>
        /// Busca el menor elemento en el subárbol de raíz x.
        /// </summary>
        /// <param name="x">Raíz del subárbol</param>
        /// <returns></returns>
        private BTreeNode<T, R> Minimo(BTreeNode<T, R> x)
        {
            BTreeNode<T, R> minimo = x;
            while (!minimo.IsLeaf)
                minimo = GetNode(minimo.Children[0]);
            return minimo;
        } 
        #endregion


        #region DISK_READ y DISK_WRITE
        /// <summary>
        ///  Escribe un nodo en disco duro.
        /// </summary>
        /// <param name="x">Nodo a escribir</param>
        public static void DISK_WRITE(BTreeNode<T, R> x)
        {
            //1-IsLeaf
            //2-CantKeys
            //3-Children
            //4-Satellite
            //5-PosFile
            //6-Keys

            long index = 0;
            byte[] bufferNode = new byte[nodeSize];
            byte[] aux;

            //Escribimos si el nodo es hoja o no.
            bufferNode[index] = BitConverter.GetBytes(x.IsLeaf)[0];
            index++;

            //Escribimos la cantidad de llaves que tiene el nodo.
            aux = BitConverter.GetBytes(x.CantKeys);
            Array.Copy(aux, 0, bufferNode, index, aux.Length);
            index += aux.Length;

            //Escribimos las referencias a los hijos
            for (int i = 0; i <= x.CantKeys; i++)
            {
                aux = BitConverter.GetBytes(x.Children[i]);
                Array.Copy(aux, 0, bufferNode, index, aux.Length);
                index += 8;
            }
            index += (x.Children.Length - (x.CantKeys + 1)) * 8;

            //Escribimos la información satélite.(R[])
            for (int i = 0; i < x.CantKeys; i++)
            {
                aux = x.Satellite[i].Save();
                Array.Copy(aux, 0, bufferNode, index, valueSize);
                index += valueSize;
            }
            index += (x.Satellite.Length - x.CantKeys) * valueSize;//nos saltamos los valores no asignados

            //Escribimos la posición del nodo en el archivo
            aux = BitConverter.GetBytes(x.PosFile);
            Array.Copy(aux, 0, bufferNode, index, aux.Length);
            index += 8;

            //Escribimos las llaves del nodo
            for (int i = 0; i < x.CantKeys; i++)
            {
                aux = x.Keys[i].Save();
                Array.Copy(aux, 0, bufferNode, index, keySize);
                index += keySize;
            }
            index += (x.Keys.Length - x.CantKeys) * keySize;

            //Se escribe la información en el archivo.
            fm.Stream.Seek(x.PosFile, SeekOrigin.Begin);
            fm.Stream.Write(bufferNode, 0, bufferNode.Length);
            fm.Stream.Flush();
        }

        /// <summary>
        ///  Lee un nodo del disco duro.
        /// </summary>
        /// <param name="posicionArchivo">Posición donde comenzar a leer el nodo.</param>
        public static BTreeNode<T, R> DISK_READ(long posicionArchivo)
        {

            //1-IsLeaf
            //2-CantKeys
            //3-Children
            //4-Satellite
            //5-PosFile
            //6-Keys

            int index = 0;
            BTreeNode<T, R> result = new BTreeNode<T, R>(posicionArchivo, t);

            //Moviendonos hacia la posición requerida en el archivo.
            fm.Stream.Seek(posicionArchivo, SeekOrigin.Begin);

            //Se lee la información correspondiente al nodo.
            byte[] bufferNode = new byte[nodeSize];
            fm.Stream.Read(bufferNode, 0, bufferNode.Length);

            //Leemos si el nodo es hoja o no.
            result.IsLeaf = BitConverter.ToBoolean(bufferNode, index);
            index++;

            //Leemos la cantidad de llaves del nodo.
            result.CantKeys = BitConverter.ToInt32(bufferNode, index);
            index += 4;


            //Leemos las referencias a los hijos
            for (int i = 0; i <= result.CantKeys; i++)
            {
                result.Children[i] = BitConverter.ToInt64(bufferNode, index);
                index += 8;
            }
            index += ((result.Keys.Length + 1) - (result.CantKeys + 1)) * 8;

            //Leemos la informacion satélite.(R[])
            byte[] subBuffer = new byte[valueSize];
            for (int i = 0; i < result.CantKeys; i++)
            {
                Array.Copy(bufferNode, index, subBuffer, 0, valueSize);
                R valor = new R();
                valor.Load(subBuffer);
                result.Satellite[i] = valor;
                index += valueSize;
            }
            index += (result.Satellite.Length - result.CantKeys) * valueSize;//nos saltamos los valores vacios

            //Leemos la posicion en el archivo
            result.PosFile = BitConverter.ToInt64(bufferNode, index);
            index += 8;

            //Leemos las llaves(T[])
            subBuffer = new byte[keySize];
            for (int i = 0; i < result.CantKeys; i++)
            {
                Array.Copy(bufferNode, index, subBuffer, 0, keySize);
                T key = new T();
                key.Load(subBuffer);
                result.Keys[i] = key;
                index += keySize;
            }

            return result;
        } 
        #endregion

        #region Levantar un nodo
        BTreeNode<T, R> GetNode(long posicionArchivo)
        {
            BTreeNode<T, R> node = RAM.Find(posicionArchivo);
            if (node != null)//si está en la cache
            {
                return node;
            }
            else //si no está en la cache
            {
                node = DISK_READ(posicionArchivo);
                RAM.Add(node);
                return node;
            }
        } 
        #endregion

        #region Properties
        //change
        public long RootPointer
        {
            get { return root.PosFile; }
        }
        #endregion

        #region InOrders
        public IEnumerable<KeyValuePair<T, R>> InOrderTreeWalk()
        {
            foreach (KeyValuePair<T, R> item in InOrderTreeWalk(root))
                yield return item;
        }
        IEnumerable<KeyValuePair<T, R>> InOrderTreeWalk(BTreeNode<T, R> node)
        {
            if (node.IsLeaf)
            {
                for (int i = 0; i < node.CantKeys; i++)
                    yield return new KeyValuePair<T, R>(node.Keys[i], node.Satellite[i]);
            }
            else
            {
                int i = 0;
                for (; i < node.CantKeys; i++)
                {
                    BTreeNode<T, R> Ci = GetNode(node.Children[i]);//DISK_READ(node.Children[i]);
                    foreach (KeyValuePair<T, R> item in InOrderTreeWalk(Ci))
                        yield return item;
                    //InOrderTreeWalk(Ci);
                    yield return new KeyValuePair<T, R>(node.Keys[i], node.Satellite[i]);
                }
                BTreeNode<T, R> last = GetNode(node.Children[i]);//DISK_READ(node.Children[i]);

                foreach (KeyValuePair<T, R> item in InOrderTreeWalk(last))
                    yield return item;
                //InOrderTreeWalk(last);
            }
        }

        public IEnumerable<KeyValuePair<T, R>> AntiInOrderTreeWalk()
        {
            foreach (KeyValuePair<T, R> item in AntiInOrderTreeWalk(root))
                yield return item;
        }
        IEnumerable<KeyValuePair<T, R>> AntiInOrderTreeWalk(BTreeNode<T, R> node)
        {
            if (node.IsLeaf)
            {
                for (int i = node.CantKeys - 1; i >= 0; i--)
                    yield return new KeyValuePair<T, R>(node.Keys[i], node.Satellite[i]);
            }
            else
            {
                int i = node.CantKeys - 1;
                for (; i >= 0; i--)
                {
                    BTreeNode<T, R> Ci = GetNode(node.Children[i + 1]);
                    foreach (KeyValuePair<T, R> item in AntiInOrderTreeWalk(Ci))
                        yield return item;
                    //AntiInOrderTreeWalk(Ci);
                    yield return new KeyValuePair<T, R>(node.Keys[i], node.Satellite[i]);
                }
                i++;//Para que i sea 0
                BTreeNode<T, R> first = GetNode(node.Children[i]);
                foreach (KeyValuePair<T, R> item in AntiInOrderTreeWalk(first))
                    yield return item;
                //AntiInOrderTreeWalk(first);
            }
        }

        #endregion

        #region Private Members
        /// <summary>
        /// Escribe en disco todos elementos que están en memoria 
        /// </summary>
        public void Save()
        {
            DISK_WRITE(root);//La escribo aqui por si alguna casualidad no esta en RAM.
            RAM.Flush();
        }
        /// <summary>
        /// Calcula el tamaño del nodo dado el keySize y el valueSize
        /// </summary>
        void CalculateNodeSize()
        {
            nodeSize = 1 +                                   //IsLeaf.
                       4 +                                   //Cantidad de llaves del nodo(CantKeys).       
                       8 * (2 * t) +                         //Referencia a los Hijos(Children).
                       valueSize * (2 * t - 1) +             //Informacion satélite(Satellite).
                       8 +                                   //Posicion en el archivo(PosFile).
                       keySize * (2 * t - 1);                //Llaves del nodo(Keys).
        }
        /// <summary>
        ///  Busca en un nodo una llava T y un valor R
        /// </summary>
        /// <param name="x">Nodo donde buscar</param>
        /// <param name="k">Llave a buscar</param>
        /// <param name="v">Valor a buscar</param>
        /// <returns></returns>
        private int buscarEnNodo(BTreeNode<T, R> x, T k, R v, bool insert)
        {
            //hacerlo logaritmico
            int lb = 0;
            int ub = x.CantKeys - 1;
            int medio;
            int compared;
            while (lb <= ub)
            {
                medio = (lb + ub) / 2;
                compared = Compare(k, x.Keys[medio], v, x.Satellite[medio]);
                if (compared < 0)
                {
                    ub = medio - 1;
                }
                else if (compared > 0)
                {
                    lb = medio + 1;
                }
                else return medio;
            }
            //for (int i = 0; i < x.CantKeys; i++)
            //{
            //    if (Compare(k, x.Keys[i], v, x.Satellite[i]) == 0)
            //        return i;
            //}
            if (insert)
                return lb;
            else
                return -1;
        }
        /// <summary>
        ///  Compara 2 pares de llave,valor y funciona igual que el CompareTo
        /// </summary>
        /// <param name="t1">Primera llave</param>
        /// <param name="t2">Segunda llave</param>
        /// <param name="r1">Primer valor</param>
        /// <param name="r2">Segundo valor</param>
        /// <returns></returns>
        private int Compare(T t1, T t2, R r1, R r2)
        {
            int result = t1.CompareTo(t2);
            if (result > 0) return 1;
            else if (result < 0) return -1;
            else
            {
                int comparacion = r1.CompareTo(r2);
                if (comparacion > 0) return 1;
                else if (comparacion < 0) return -1;
                else return 0;
            }
        }
        #endregion

    }
    /// <summary>
    /// Estructura de Datos que simula una memoria cache. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="R"></typeparam>
    internal class CacheMemory<T, R>
        where T : IComparable, IGuardable, new()
        where R : IComparable, IGuardable, new()
    {

        #region Campos
        LinkedList<BTreeNode<T, R>> cache;
        int capacity;
        #endregion

        #region Constructores
        /// <summary>
        /// Construye una nueva cache
        /// </summary>
        /// <param name="capacity">Capacidad de la cache</param>
        public CacheMemory(int capacity)
        {
            cache = new LinkedList<BTreeNode<T, R>>();
            this.capacity = capacity;
        }
        #endregion

        #region Métodos
       
        /// <summary>
        /// Agrega un nodo a la cache
        /// </summary>
        /// <param name="x">Nodo a agregar</param>
        public void Add(BTreeNode<T, R> x)
        {
            if (cache.Count == capacity)
            {
                BTree<T, R>.DISK_WRITE(cache.Last.Value);
                cache.RemoveLast();
            }
            cache.AddFirst(x);
        }
        /// <summary>
        /// Indica si un nodo está en la cache y lo devuelve. 
        /// </summary>
        /// <param name="posFile">Posición del nodo en disco duro</param>
        /// <returns></returns>
        public BTreeNode<T, R> Contains(long posFile)
        {
            foreach (var item in cache)
            {
                if (item.PosFile == posFile)
                    return item;
            }
            return null;
        }
        /// <summary>
        /// Devuelve un nodo de la cache
        /// </summary>
        /// <param name="posFile">Posición del nodo en disco duro</param>
        /// <returns></returns>
        public BTreeNode<T, R> Find(long posFile)
        {
            BTreeNode<T, R> result = Contains(posFile);
            if (result == null)
                return null;
            cache.Remove(result);
            cache.AddFirst(result);
            return cache.First.Value;
        }
        /// <summary>
        /// Escribe en disco todos los nodos de la cache
        /// </summary>
        public void Flush()
        {
            foreach (var item in cache)
                BTree<T, R>.DISK_WRITE(item);
            cache = new LinkedList<BTreeNode<T, R>>();
        }
        /// <summary>
        /// Actualiza un nodo que haya sufrido modificaciones
        /// </summary>
        /// <param name="x"></param>
        public void Update(BTreeNode<T, R> x)
        {
            BTreeNode<T, R> result = Contains(x.PosFile);
            if (result == null)//si no esta en la cache
                Add(x);
            else//si esta en la cache
                result = x;
        }
        /// <summary>
        /// Elimina al nodo de la cache, pero solo cuando su memoria es liberada
        /// </summary>
        /// <param name="x"></param>
        public void Remove(BTreeNode<T, R> x)
        {
            BTreeNode<T, R> result = Contains(x.PosFile);
            if (result == null)
                return;
            cache.Remove(result);
        }

        #endregion

        #region Propiedades

        public int Count
        {
            get { return cache.Count; }
        }
        public int Capacity
        {
            get { return capacity; }
        }

        #endregion
    }
}
