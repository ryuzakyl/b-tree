using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FileHandler
{
    /// <summary>
    ///  Es el encargado de manejar la memoria del archivo usado para la Estructura de 
    ///  Datos B-Tree
    /// </summary>
    public class FileManager
    {

        #region Campos
        /// <summary>
        /// Stream subyacente
        /// </summary>
        private FileStream writer;

        /// <summary>
        /// Puntero al 1er espacio libre en memoria, si es 0 hay que ir al final del archivo.
        /// </summary>
        long freeMemory;

        /// <summary>
        ///  HeapStart puntero al comienzo de la informacion dinamica(los BTreeNode)
        /// </summary>
        long hs;

        /// <summary>
        /// HeapEnd puntero al fin de la memoria dinámica
        /// </summary>
        long he;

        /// <summary>
        /// Tamaño del bloque del archivo
        /// </summary>
        int blockSize;
        #endregion

        #region Constructores
        /// <summary>
        /// Crea un FileManager
        /// </summary>
        /// <param name="fileWriter">Stream subyacente</param>
        /// <param name="blockSize">Tamaño de bloque</param>
        public FileManager(FileStream fileWriter,int blockSize)
        {
            writer = fileWriter;
            this.blockSize = blockSize;
            Load();
        }

        #endregion

        #region Métodos
        /// <summary>
        ///  Encargado de reservar memoria
        /// </summary>
        /// <returns>Devuelve un long donde la memoria esta libre</returns>
        public long Alloc()
        {
            if (freeMemory == 0)
            {
                long tmp = he;
                he += blockSize;
                return tmp;//se busca en el final del archivo
            }
            else 
            {
                long tmp = freeMemory;
                writer.Seek(freeMemory, SeekOrigin.Begin);
                
                //Se lee la sgte memoria libre
                byte[] buffer=new byte[8];
                writer.Read(buffer, 0, buffer.Length);
                long nextLibre = BitConverter.ToInt64(buffer, 0);
                
                freeMemory = nextLibre;//se guarda la sgte memoria libre
                
                return tmp;//Se devuelve la memoria libre
            }
        }
        /// <summary>
        /// Encargado de liberar un espacio en memoria.
        /// </summary>
        /// <param name="memory">Espacio en memoria a liberar</param>
        public void Free(long memory)
        {
            long tmp = freeMemory;
            freeMemory = memory;
            writer.Seek(memory, SeekOrigin.Begin);
            writer.Write(BitConverter.GetBytes(tmp), 0, 8);
            writer.Flush();
        }
        /// <summary>
        /// Encargado de guardar toda la informacián necesaria.
        /// </summary>
        public void Save()
        {
            byte[] buffer = new byte[0];
            
            //Escribimos el 1er espacio libre de memoria
            writer.Position = 0;
            buffer = BitConverter.GetBytes(freeMemory);
            writer.Write(buffer, 0, 8);

            //Escribimos el comienzo de la memoria dinamica.
            buffer = BitConverter.GetBytes(hs);
            writer.Write(buffer, 0, 8);

            //Escribimos el fin de la memoria dinamica
            buffer = BitConverter.GetBytes(he);
            writer.Write(buffer, 0, 8);

            writer.Flush();//escribimos en el archivo.
            writer.Close();
        }
        /// <summary>
        /// Encargado de leer la información necesaria.
        /// </summary>
        private void Load()
        {
            byte[] buffer=new byte[8];

            //Se lee la 1era memoria libre
            writer.Position = 0;
            writer.Read(buffer, 0, 8);
            freeMemory = BitConverter.ToInt64(buffer, 0);

            //Se lee el comienzo de la memoria dinamica.
            writer.Read(buffer, 0, 8);
            hs = BitConverter.ToInt64(buffer, 0);

            //Se lee el fin de la memoria dinamica
            writer.Read(buffer, 0, 8);
            he = BitConverter.ToInt64(buffer, 0);
        }
        #endregion

        #region Propiedades
        /// <summary>
        /// Path del archivo
        /// </summary>
        public string Path
        {
            get { return writer.Name; }
        }
        /// <summary>
        /// Stream subyacente
        /// </summary>
        public FileStream Stream
        {
            get 
            {
                return writer;
            }
        }
        /// <summary>
        /// Puntero al comienzo de la memoria dinámica
        /// </summary>
        public long HeapStart
        {
            get { return hs; }
            set { hs = value; }
        }
        /// <summary>
        /// Puntero al fin de la memoria dinámica
        /// </summary>
        public long HeapEnd
        {
            get { return he; }
            set { he = value; }
        }
        /// <summary>
        /// Tamaño de bloque del archivo
        /// </summary>
        public int BlockSize
        {
            set { blockSize = value; }
        }
        #endregion
    }
}
