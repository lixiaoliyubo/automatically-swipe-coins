using System.Collections.Generic;
using System.Drawing;
using PaddleOCRSharp;

namespace AdAuto.Extensions.Helper
{
    /// <summary>
    /// 记录上一次点击的信息，用于防止重复点击
    /// </summary>
    public class LastClickInfo
    {
        /// <summary>
        /// 上一次点击的坐标点列表
        /// </summary>
        public List<OCRPoint>? Points { get; set; }

        /// <summary>
        /// 上一次点击的位置描述
        /// </summary>
        public string Position { get; set; } = string.Empty;

        /// <summary>
        /// 判断当前点击是否与上一次点击相同
        /// </summary>
        /// <param name="points">当前点击的坐标点列表</param>
        /// <param name="pos">当前点击的位置描述</param>
        /// <returns>如果当前点击与上一次点击完全相同则返回true，否则返回false</returns>
        public bool IsSameClick(List<OCRPoint>? points, string pos)
        {
            if (points == null && Points == null)
            {
                return Position == pos;
            }
            if (points == null || Points == null)
            {
                return false;
            }
            if (points.Count != Points.Count)
            {
                return false;
            }

            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].X != Points[i].X || points[i].Y != Points[i].Y)
                {
                    return false;
                }
            }

            return Position == pos;
        }

        /// <summary>
        /// 更新上一次点击的信息
        /// </summary>
        /// <param name="points">新的点击坐标点列表</param>
        /// <param name="pos">新的点击位置描述</param>
        public void Update(List<OCRPoint>? points, string pos)
        {
            Points = points;
            Position = pos;
        }
    }
}