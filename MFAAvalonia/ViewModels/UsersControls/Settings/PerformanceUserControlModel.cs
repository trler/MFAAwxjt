using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using MaaFramework.Binding;
using MFAAvalonia.Configuration;
using MFAAvalonia.Extensions.MaaFW;
using MFAAvalonia.Helper.Converters;
using MFAAvalonia.ViewModels.Other;
using System;
using System.Collections.Generic;

#if WINDOWS
using SharpDX;
using SharpDX.DXGI;
#endif

namespace MFAAvalonia.ViewModels.UsersControls.Settings;

public partial class PerformanceUserControlModel : ViewModelBase
{
    [ObservableProperty] public bool _isDirectMLSupported = OperatingSystem.IsWindows();

    [ObservableProperty] private bool _useDirectML = ConfigurationManager.Current.GetValue(ConfigurationKeys.UseDirectML, false);

    public class DirectMLAdapterInfo
    {
        public int AdapterId { get; set; } // 与EnumAdapters1索引一致
        public string AdapterName { get; set; }
        public bool IsDirectMLCompatible { get; set; }
    }

#if WINDOWS
    public static List<DirectMLAdapterInfo> GetCompatibleAdapters()
    {
        var adapters = new List<DirectMLAdapterInfo>();
        using (var factory = new Factory1())
        {
            for (int index = 0; index < factory.GetAdapterCount1(); index++)
            {
                try
                {
                    using (var adapter = factory.GetAdapter1(index))
                    {
                        var desc = adapter.Description1;
                        // 关键：检查适配器是否支持Direct3D 12（DirectML必要条件）
                        var isD3D12Supported = adapter.IsInterfaceSupported<SharpDX.Direct3D12.Device>();

                        adapters.Add(new DirectMLAdapterInfo
                        {
                            AdapterId = index,
                            AdapterName = desc.Description.Trim(),
                            IsDirectMLCompatible = isD3D12Supported
                        });
                    }
                }
                catch (SharpDXException) { continue; } // 跳过无法查询的适配器
            }
        }
        return adapters;
    }
#endif
    partial void OnUseDirectMLChanged(bool value)
    {
        if (value)
        {
#if WINDOWS
            if (GpuOptions.Count == 2)
            {
                var gpus = ConfigurationManager.Current.GetValue(ConfigurationKeys.GPUs, new List<LocalizationViewModel>());
                if (gpus.Count <= 0)
                {
                    var adapters = GetCompatibleAdapters();
                    foreach (var adapter in adapters)
                    {
                        gpus.Add(new LocalizationViewModel(adapter.AdapterName)
                        {
                            Other = new GpuDeviceOption(adapter.AdapterId)
                        });
                    }
                    ConfigurationManager.Current.GetValue(ConfigurationKeys.GPUs, gpus);
                }

                GpuOptions.InsertRange(1, gpus);
            }
#endif
        }
        else
        {
            if (GpuOptions.Count != 2)
            {
                while (GpuOptions.Count > 2)
                {
                    GpuOptions.RemoveAt(1);
                }
                GpuOption = GpuOptions[0].Other as GpuDeviceOption;
            }
        }
    }

    public class GpuDeviceOption
    {
        public static GpuDeviceOption Auto = new(InferenceDevice.Auto);
        public static GpuDeviceOption CPU = new(InferenceDevice.CPU);
        public static GpuDeviceOption GPU0 = new(InferenceDevice.GPU0);
        public static GpuDeviceOption GPU1 = new(InferenceDevice.GPU1);
        public GpuDeviceOption(InferenceDevice device)
        {
            Device = device;
            IsDirectML = false;
        }
        public GpuDeviceOption(int id)
        {
            Id = id;
            IsDirectML = true;
        }
        public InferenceDevice Device;
        public int Id;
        public bool IsDirectML;
    }

    [ObservableProperty] private AvaloniaList<LocalizationViewModel> _gpuOptions =
    [
        new("GpuOptionAuto")
        {
            Other = GpuDeviceOption.Auto,
        },
        new("GpuOptionDisable")
        {
            Other = GpuDeviceOption.CPU
        }
    ];

    [ObservableProperty] private GpuDeviceOption _gpuOption = ConfigurationManager.Current.GetValue(ConfigurationKeys.GPUOption, GpuDeviceOption.Auto, GpuDeviceOption.GPU0, new UniversalEnumConverter<InferenceDevice>());

    partial void OnGpuOptionChanged(GpuDeviceOption value) => HandlePropertyChanged(ConfigurationKeys.GPUOption, value, v =>
    {
        ChangeGpuOption(MaaProcessor.Instance.MaaTasker?.Resource, value);
    });

    public void ChangeGpuOption(MaaResource? resource, GpuDeviceOption? option)
    {
        if (option != null && resource != null)
        {
            if (option.IsDirectML)
            {
                resource.SetOption_InferenceExecutionProvider(InferenceExecutionProvider.DirectML);
                resource.SetOption_InferenceDevice(option.Id);
            }
            else
            {
                resource.SetOption_InferenceExecutionProvider(InferenceExecutionProvider.Auto);
                resource.SetOption_InferenceDevice(option.Device);
            }
        }
    }
}
