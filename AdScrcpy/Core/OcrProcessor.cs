using System;
using System.Drawing;
using PaddleOCRSharp;

namespace AdScrcpy.Core
{
    public class OcrProcessor : IDisposable
    {
        private readonly OCRModelConfig _modelConfig;
        private readonly PaddleOCREngine _engine;

        public OcrProcessor()
        {
            _modelConfig = new OCRModelConfig();
            // 使用默认参数
            var oCRParameter = new OCRParameter
            {
                cpu_math_library_num_threads = 10, // 预测并发线程数
                enable_mkldnn = true, // web部署该值建议设置为0, 否则出错，内存如果使用很大，建议该值也设置为0.
                cls = false, // 是否执行文字方向分类；默认false
                det = true, // 是否开启方向检测，用于检测识别180旋转
                use_angle_cls = false, // 是否开启方向检测，用于检测识别180旋转
                det_db_score_mode = true // 是否使用多段线，即文字区域是用多段线还是用矩形
            };

            // 建议程序全局初始化一次即可，不必每次识别都初始化，容易报错。
            _engine = new PaddleOCREngine(null, oCRParameter);
        }

        public OCRResult ProcessImage(Bitmap bitmap)
        {
            return _engine.DetectText(bitmap);
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }
    }
}