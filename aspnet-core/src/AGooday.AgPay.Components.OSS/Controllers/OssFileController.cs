﻿using AGooday.AgPay.Common.Exceptions;
using AGooday.AgPay.Common.Models;
using AGooday.AgPay.Common.Utils;
using AGooday.AgPay.Components.OSS.Extensions;
using AGooday.AgPay.Components.OSS.Models;
using AGooday.AgPay.Components.OSS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AGooday.AgPay.Components.OSS.Controllers
{
    [ApiController, Authorize]
    [Route("api/ossFiles")]
    public class OssFileController : ControllerBase
    {
        private readonly ILogger<OssFileController> logger;
        private readonly IOssService ossService;

        public OssFileController(ILogger<OssFileController> logger, IOssServiceFactory ossServiceFactory)
        {
            this.logger = logger;
            this.ossService = ossServiceFactory.GetService();
        }

        /// <summary>
        /// 上传表单参数
        /// </summary>
        /// <param name="bizType"></param>
        /// <param name="fileName"></param>
        /// <param name="fileSize"></param>
        /// <returns></returns>
        /// <exception cref="BizException"></exception>
        [HttpGet, Route("{bizType}")]
        public async Task<ApiRes> GetUploadFormParamsAsync(string bizType, string fileName, long fileSize)
        {
            try
            {
                OssFileConfig ossFileConfig = OssFileConfig.GetOssFileConfigByBizType(bizType);

                //1. 判断bizType 是否可用
                if (ossFileConfig == null)
                {
                    throw new BizException("类型有误");
                }

                // 2. 判断文件是否支持
                string suffix = Path.GetExtension(fileName);
                string fileSuffix = FileUtil.GetFileSuffix(fileName, false);
                if (!ossFileConfig.IsAllowFileSuffix(fileSuffix))
                {
                    throw new BizException("上传文件格式不支持！");
                }

                // 3. 判断文件大小是否超限
                if (!ossFileConfig.IsMaxSizeLimit(fileSize))
                {
                    throw new BizException($"上传大小请限制在[{ossFileConfig.MaxSize / 1024 / 1024}M]以内！");
                }

                // 新文件地址 (xxx/xxx.jpg 格式)
                string saveDirAndFileName = Path.Combine(bizType, $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}");
                var formParams = await ossService.GetUploadFormParamsAsync(ossFileConfig.OssSavePlaceEnum, bizType, saveDirAndFileName);
                return ApiRes.Ok(formParams);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"upload error, fileName = {fileName}");
                throw new BizException(ApiCode.SYSTEM_ERROR, e.Message);
            }
        }

        /// <summary>
        /// 上传文件 （单文件上传）
        /// </summary>
        /// <param name="file"></param>
        /// <param name="bizType"></param>
        /// <returns></returns>
        /// <exception cref="BizException"></exception>
        [HttpPost, Route("{bizType}")]
        public async Task<ApiRes> SingleFileUploadAsync([FromForm] IFormFile file, string bizType)
        {
            if (file == null)
            {
                return ApiRes.Fail(ApiCode.SYSTEM_ERROR, "选择文件不存在");
            }
            try
            {
                OssFileConfig ossFileConfig = OssFileConfig.GetOssFileConfigByBizType(bizType);

                //1. 判断bizType 是否可用
                if (ossFileConfig == null)
                {
                    throw new BizException("类型有误");
                }

                // 2. 判断文件是否支持
                string suffix = Path.GetExtension(file.FileName);
                string fileSuffix = FileUtil.GetFileSuffix(file.FileName, false);
                if (!ossFileConfig.IsAllowFileSuffix(fileSuffix))
                {
                    throw new BizException("上传文件格式不支持！");
                }

                // 3. 判断文件大小是否超限
                if (!ossFileConfig.IsMaxSizeLimit(file.Length))
                {
                    throw new BizException($"上传大小请限制在[{ossFileConfig.MaxSize / 1024 / 1024}M]以内！");
                }

                // 新文件地址 (xxx/xxx.jpg 格式)
                string saveDirAndFileName = Path.Combine(bizType, $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}");
                string url = await ossService.Upload2PreviewUrlAsync(ossFileConfig.OssSavePlaceEnum, file, saveDirAndFileName);
                return ApiRes.Ok(url);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"upload error, fileName = {file.FileName}");
                throw new BizException(ApiCode.SYSTEM_ERROR, e.Message);
            }
        }

        [HttpGet, Route("get")]
        public IEnumerable<int> Get()
        {
            return Enumerable.Range(1, 5).Select(index => Random.Shared.Next(index, 55)).ToArray();
        }
    }
}
