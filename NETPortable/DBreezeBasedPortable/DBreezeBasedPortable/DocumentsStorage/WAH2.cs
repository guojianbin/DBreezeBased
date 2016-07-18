﻿/* 
  Copyright (C) 2014 dbreeze.tiesky.com / Alex Solovyov / Ivars Sudmalis.
  It's a free software for those, who thinks that it should be free.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using DBreeze.Utils;
using DBreezeBased.Compression;

namespace DBreezeBased.DocumentsStorage
{
    /// <summary>
    /// 
    /// </summary>
    public class WAH2
    {


        byte[] bt = null;
        byte currentProtocol = 1;

        /// <summary>
        /// 
        /// </summary>
        public WAH2()
        {
        }

        /// <summary>
        /// Must be supplied CompressedByteArray taken from GetCompressedByteArray function
        /// </summary>
        /// <param name="array"></param>
        public WAH2(byte[] array)
        {
            if (array == null || array.Length < 1)
                return;

            //First byte is SByte showing by module(ABS) version of the protocol
            //if <0 then compressed
            //bt = Substring(array, 2, array.Length);
            bt = array.Substring(2,array.Length);
            if (array[1] == 1)
                bt = bt.DecompressGZip();
            
        }


        /// <summary>
        /// Working byte[]
        /// </summary>
        /// <returns></returns>
        public byte[] GetUncompressedByteArray()
        {
            if (bt == null || bt.Length == 0)
                return new byte[0];

            return bt;
        }

        /// <summary>
        /// With extra protocol definition, ready for save into DB
        /// </summary>
        /// <returns></returns>
        public byte[] GetCompressedByteArray()
        {
            if (bt == null || bt.Length == 0)
                return null;
              
            //Compression is currently off, cause the whole dataBlock will be compressed and while searching we don't need to decompress every found word's WAH again
            //Compressing if more then 100 bytes
            //if (bt.Length > 100)
            //{
            //    byte[] tbt = bt.CompressGZip();

            //    if(bt.Length<=tbt.Length)
            //        return new byte[] { currentProtocol }.ConcatMany(new byte[] { 0 }, bt);
                
            //    return new byte[] { currentProtocol }.ConcatMany(new byte[] { 1 }, tbt);
            //}

         
            return new byte[] { currentProtocol }.ConcatMany(new byte[] { 0 }, bt);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void Add(int index, bool value)
        {
            int byteNumber = Convert.ToInt32(index / 8);
            int rest = index % 8;

            int btLen = 0;
            if (bt != null)
                btLen = bt.Length;

            if (byteNumber > (btLen - 1))
                Resize(byteNumber + 1);

            byte mask = (byte)(1 << rest);

            if (value)
                bt[byteNumber] |= mask; // set to 1
            else
                bt[byteNumber] &= (byte)~mask;  // Set to zero

            //bool isSet = (bytes[byteIndex] & mask) != 0;
            //int bitInByteIndex = bitIndex % 8;
            //int byteIndex = bitIndex / 8;
            //byte mask = (byte)(1 << bitInByteIndex);
            //bool isSet = (bytes[byteIndex] & mask) != 0;
            //// set to 1
            //bytes[byteIndex] |= mask;
            //// Set to zero
            //bytes[byteIndex] &= ~mask;
            //// Toggle
            //bytes[byteIndex] ^= mask;            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="len"></param>
        void Resize(int len)
        {
            byte[] btNew = new byte[len];
            if (bt == null)
            {
                bt = btNew;
                return;
            }

            for (int i = 0; i < bt.Length; i++)
            {
                btNew[i] = bt[i];
            }

            bt = btNew;
            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool Contains(int index)
        {
            int btLen = 0;
            if (bt != null)
                btLen = bt.Length;

            if (btLen < 1)
                return false;

            int byteNumber = Convert.ToInt32(index / 8);

            if (byteNumber > (btLen - 1))
                return false;

            int rest = index % 8;
            byte mask = (byte)(1 << rest);
            return (bt[byteNumber] & mask) != 0;
        }

        /// <summary>
        /// Using OR logic: 1|1 = 1|0 = 1; 0|0 = 0
        /// </summary>
        /// <param name="indexesToMerge"></param>
        /// <returns></returns>
        public static byte[] MergeAllUncompressedIntoOne(List<byte[]> indexesToMerge)
        {
            //if (indexesToMerge == null || indexesToMerge.Count() < 1)
            //    return null;
            int MaxLenght = indexesToMerge.Max(r => r.Length);
            byte[] res = new byte[MaxLenght];
            
            foreach (var bt in indexesToMerge)
            {
                for (int i = 0; i < bt.Length; i++)
                {
                    res[i] |= bt[i];
                }

            }

            return res;
        }

        /// <summary>
        /// Technical if already in DB
        /// </summary>
        public bool ExistsInDB = false;

        ///// <summary>
        ///// Returns first added document first (sort by ID asc)
        ///// </summary>
        ///// <param name="indexesToCheck"></param>
        ///// <returns></returns>
        //public static IEnumerable<uint> TextSearch_AND_logic(List<byte[]> indexesToCheck)
        //{
        //    int MinLenght = indexesToCheck.Min(r => r.Length);
        //    byte res = 0;
        //    uint docId = 0;
        //    byte mask = 0;

        //    for (int i = 0; i < MinLenght; i++)
        //    {
        //        res = 255;
        //        foreach (var wah in indexesToCheck)
        //        {
        //            res &= wah[i];
        //        }

        //        for (int j = 0; j < 8; j++)
        //        {
        //            mask = (byte)(1 << j);

        //            if ((res & mask) != 0)
        //                yield return docId;

        //            docId++;
        //        }
        //    }
        //}

        /// <summary>
        /// Returns last added documents first
        /// </summary>
        /// <param name="indexesToCheck"></param>
        /// <returns></returns>
        public static IEnumerable<uint> TextSearch_AND_logic(List<byte[]> indexesToCheck)
        {
            int MinLenght = indexesToCheck.Min(r => r.Length);
            byte res = 0;
            uint docId = Convert.ToUInt32(MinLenght * 8) - 1;
            byte mask = 0;

            for (int i = MinLenght - 1; i >= 0; i--)
            {
                res = 255;
                foreach (var wah in indexesToCheck)
                {
                    res &= wah[i];
                }
                
                for (int j = 7; j >= 0; j--)
                {
                    mask = (byte)(1 << j);

                    if ((res & mask) != 0)
                        yield return (uint)docId;

                    docId--;
                }
            }
        }


        ///// <summary>
        ///// SOrt by ID desc
        ///// </summary>
        ///// <param name="indexesToCheck"></param>
        ///// <param name="maximalReturnQuantity"></param>
        ///// <returns></returns>
        //public static IEnumerable<uint> TextSearch_OR_logic(List<byte[]> indexesToCheck, int maximalReturnQuantity)
        //{
        //    int MaxLenght = indexesToCheck.Max(r => r.Length);
        //    uint docId = 0;
        //    byte mask = 0;
        //    int added = 0;
        //    int[] el = new int[8];

        //    SortedDictionary<int, List<uint>> d = new SortedDictionary<int, List<uint>>();
        //    List<uint> docLst = null;

        //    for (int i = 0; i < MaxLenght; i++)
        //    {
        //        foreach (var wah in indexesToCheck)
        //        {
        //            if (i > (wah.Length - 1))
        //                continue;

        //            for (int j = 0; j < 8; j++)
        //            {
        //                mask = (byte)(1 << j);
        //                if ((wah[i] & mask) != 0)
        //                    el[j] += 1;
        //            }
        //        }

        //        //Here we analyze el array
        //        for (int j = 0; j < 8; j++)
        //        {
        //            //el[j] contains quantity of occurance
        //            if (el[j] > 0)
        //            {
        //                if (!d.TryGetValue(el[j], out docLst))
        //                    docLst = new List<uint>();

        //                added++;
        //                docLst.Add(docId);

        //                d[el[j]] = docLst;
        //            }

        //            el[j] = 0;
        //            docId++;
        //        }

        //        if (added > maximalReturnQuantity)
        //            break;
        //    }

        //    foreach (var ret in d.OrderByDescending(r => r.Key))
        //        foreach (var docs in ret.Value)
        //            yield return docs;
        //}


        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexesToCheck"></param>
        /// <param name="maximalReturnQuantity"></param>
        /// <returns></returns>
        public static IEnumerable<uint> TextSearch_OR_logic(List<byte[]> indexesToCheck, int maximalReturnQuantity)
        {
            int MaxLenght = indexesToCheck.Max(r => r.Length);
            uint docId = Convert.ToUInt32(MaxLenght * 8) - 1;
            byte mask = 0;
            int added = 0;
            int[] el = new int[8];

            SortedDictionary<int, List<uint>> d = new SortedDictionary<int, List<uint>>();
            List<uint> docLst = null;

            for (int i = MaxLenght - 1; i >= 0; i--)
            {
                foreach (var wah in indexesToCheck)
                {
                    if (i > (wah.Length - 1))
                        continue;

                    //for (int j = 0; j < 8; j++)
                    for (int j = 7; j >= 0; j--)
                    {
                        mask = (byte)(1 << j);
                        if ((wah[i] & mask) != 0)
                            el[j] += 1;
                    }
                }

                //Here we analyze el array
                //for (int j = 0; j < 8; j++)
                for (int j = 7; j >= 0; j--)
                {
                    //el[j] contains quantity of occurance
                    if (el[j] > 0)
                    {
                        if (!d.TryGetValue(el[j], out docLst))
                            docLst = new List<uint>();

                        added++;
                        yield return docId;
                        //docLst.Add(docId);

                        d[el[j]] = docLst;
                    }

                    el[j] = 0;
                    docId--;
                }

                if (added > maximalReturnQuantity)
                    break;
            }

            //foreach (var ret in d.OrderByDescending(r => r.Key))
            //    foreach (var docs in ret.Value)
            //        yield return docs;
        }

    }
}
