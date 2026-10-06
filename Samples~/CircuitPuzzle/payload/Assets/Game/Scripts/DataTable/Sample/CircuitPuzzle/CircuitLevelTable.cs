//------------------------------------------------------------
//------------------------------------------------------------
// 此文件由工具自动生成，请勿直接修改。
// 生成时间：__DATA_TABLE_CREATE_TIME__
//------------------------------------------------------------

using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityGameFramework.Runtime;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName | Obfuz.ObfuzScope.MethodName)]
#endif
/// <summary>
/// 电路拼图关卡表
/// </summary>
public class CircuitLevelTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 关卡编号
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 固定随机种子
        /// </summary>
        public int Seed
        {
            get;
            private set;
        }

        /// <summary>
        /// 棋盘边长
        /// </summary>
        public int BoardSize
        {
            get;
            private set;
        }

        /// <summary>
        /// 推荐步数
        /// </summary>
        public int TargetMoves
        {
            get;
            private set;
        }

        public override bool ParseDataRow(string dataRowString, object userData)
        {
            string[] columnStrings = dataRowString.Split(DataTableExtension.DataSplitSeparators);
            for (int i = 0; i < columnStrings.Length; i++)
            {
                columnStrings[i] = columnStrings[i].Trim(DataTableExtension.DataTrimSeparators);
            }

            int index = 0;
            index++;
            m_Id = int.Parse(columnStrings[index++]);
            index++;
            Seed = int.Parse(columnStrings[index++]);
            BoardSize = int.Parse(columnStrings[index++]);
            TargetMoves = int.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    Seed = binaryReader.Read7BitEncodedInt32();
                    BoardSize = binaryReader.Read7BitEncodedInt32();
                    TargetMoves = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
