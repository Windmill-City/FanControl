
using System;
using System.Runtime.InteropServices;


public class NV_Queries
{
    [DllImport("NV_Helper.dll")]
    public static extern bool nv_init();
    [DllImport("NV_Helper.dll")]
    public static extern bool nv_shutdown();
    [DllImport("NV_Helper.dll")]
    public static extern bool nv_getCount(out uint count);
    [DllImport("NV_Helper.dll")]
    public static extern bool nv_getNameByIndex(int index, ref Byte name, out int size);
    [DllImport("NV_Helper.dll")]
    public static extern bool nv_getPowerUsageByIndex(int index, out uint power);
    [DllImport("NV_Helper.dll")]
    public static extern bool nv_getTemperatureByIndex(int index, out uint temp);
    [DllImport("NV_Helper.dll")]
    public static extern bool nv_getUtilizationRatesByIndex(int index, out uint u_memory, out uint u_gpu);
    [DllImport("NV_Helper.dll")]
    public static extern bool nv_getEncoderUtilizationByIndex(int index, out uint uiVidEncoderUtil, out uint uiVideEncoderLastSamplePeriodUs);
    [DllImport("NV_Helper.dll")]
    public static extern bool nv_getDecoderUtilizationByIndex(int index, out uint uiVidDecoderUtil, out uint uiVideDeoderLastSamplePeriodUs);
    [DllImport("NV_Helper.dll")]
    public static extern bool nv_getMemoryInfoByIndex(int index, out uint ulFrameBufferTotalMBytes, out uint ulFrameBufferFreeMBytes);
}

