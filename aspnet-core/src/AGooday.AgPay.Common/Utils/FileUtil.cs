﻿using AGooday.AgPay.Common.Constants;
using AGooday.AgPay.Common.Exceptions;

namespace AGooday.AgPay.Common.Utils
{
    /// <summary>
    /// 文件工具类
    /// </summary>
    public class FileUtil
    {
        /// <summary>
        /// 获取文件的后缀名
        /// </summary>
        /// <param name="fullFileName"></param>
        /// <param name="appendDot">是否拼接</param>
        /// <returns></returns>
        public static string GetFileSuffix(string fullFileName, bool appendDot)
        {
            if (fullFileName == null || fullFileName.IndexOf('.') < 0 || fullFileName.Length <= 1)
            {
                return "";
            }
            return string.Concat(appendDot ? "." : "", fullFileName.AsSpan(fullFileName.LastIndexOf('.') + 1));
        }


        /// <summary>
        /// 获取有效的图片格式， 返回null： 不支持的图片类型
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// <exception cref="BizException"></exception>
        public static string GetImgSuffix(string filePath)
        {
            string suffix = GetFileSuffix(filePath, false).ToLower();
            if (CS.ALLOW_UPLOAD_IMG_SUFFIX.Contains(suffix))
            {
                return suffix;
            }
            throw new BizException("不支持的图片类型");
        }
    }
}
