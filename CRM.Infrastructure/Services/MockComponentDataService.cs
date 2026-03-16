using CRM.Core.Interfaces;
using CRM.Core.Models.Component;

namespace CRM.Infrastructure.Services
{
    /// <summary>
    /// 物料数据获取服务 - 模拟数据实现
    /// 用于在真实 API（Nexar）账号未就绪时提供完整的功能演示
    /// 
    /// 替换为真实 API 时：
    /// 1. 创建 NexarComponentDataService : IComponentDataService
    /// 2. 在 ServiceCollectionExtensions 中将注册改为 NexarComponentDataService
    /// 3. 此文件可保留用于单元测试
    /// </summary>
    public class MockComponentDataService : IComponentDataService
    {
        public string SourceName => "Mock";

        public Task<ComponentDetailDto?> FetchByMpnAsync(string mpn)
        {
            // 模拟网络延迟
            // await Task.Delay(300);

            var upperMpn = mpn.ToUpperInvariant();

            // 根据型号返回对应的模拟数据，未知型号返回通用模板
            var result = upperMpn switch
            {
                "LM358N" => BuildLm358nData(),
                "LM358" => BuildLm358nData(),
                "NE555P" => BuildNe555pData(),
                "NE555" => BuildNe555pData(),
                "STM32F103C8T6" => BuildStm32Data(),
                _ => BuildGenericData(mpn)
            };

            return Task.FromResult<ComponentDetailDto?>(result);
        }

        // ─────────────────────────────────────────────────────────
        // LM358N 双运算放大器
        // ─────────────────────────────────────────────────────────
        private static ComponentDetailDto BuildLm358nData() => new()
        {
            Mpn = "LM358N",
            ManufacturerName = "Texas Instruments",
            ShortDescription = "Dual Operational Amplifier, Low Power, General Purpose",
            Description = "The LM358N is primarily designed for commercial and light industrial applications. Its temperature range of -40°C to +85°C makes it suitable for environments where moderate temperature variations are expected. It offers reliable performance for general-purpose signal conditioning but is not typically qualified for stringent automotive or military standards requiring extended temperature ranges or enhanced reliability.",
            LifecycleStatus = "Active",
            PackageType = "DIP-8",
            IsRoHSCompliant = true,
            DataSource = "Mock",
            FetchedAt = DateTime.UtcNow,
            Specs = new List<ComponentSpec>
            {
                new() { AttributeName = "Number of Channels", DisplayValue = "2" },
                new() { AttributeName = "Supply Voltage (Min)", DisplayValue = "3 V" },
                new() { AttributeName = "Supply Voltage (Max)", DisplayValue = "32 V" },
                new() { AttributeName = "Gain Bandwidth Product", DisplayValue = "0.7 MHz" },
                new() { AttributeName = "Slew Rate", DisplayValue = "0.3 V/µs" },
                new() { AttributeName = "Input Offset Voltage (Max)", DisplayValue = "7 mV" },
                new() { AttributeName = "Input Bias Current (Max)", DisplayValue = "250 nA" },
                new() { AttributeName = "Operating Temperature (Min)", DisplayValue = "-40°C" },
                new() { AttributeName = "Operating Temperature (Max)", DisplayValue = "+85°C" },
                new() { AttributeName = "Output Current (Max)", DisplayValue = "40 mA" }
            },
            Sellers = new List<DistributorPricing>
            {
                new()
                {
                    DistributorName = "DigiKey",
                    DistributorUrl = "https://www.digikey.com/en/products/filter/linear-amps/687?s=N4IgTCBcDaIIwFYBsAOAtHAzABjQJgHYBOAGhAF0BfIA",
                    InStock = 250000,
                    IsAuthorized = true,
                    PartNumber = "LM358NE4-ND",
                    MinOrderQty = "1",
                    LeadTime = "In Stock",
                    PriceBreaks = new List<PriceBreak>
                    {
                        new() { Quantity = 1, Price = 0.2500m, Currency = "USD" },
                        new() { Quantity = 10, Price = 0.1890m, Currency = "USD" },
                        new() { Quantity = 100, Price = 0.1500m, Currency = "USD" },
                        new() { Quantity = 1000, Price = 0.1200m, Currency = "USD" },
                        new() { Quantity = 10000, Price = 0.1000m, Currency = "USD" }
                    }
                },
                new()
                {
                    DistributorName = "Mouser",
                    DistributorUrl = "https://www.mouser.com/c/?q=LM358N",
                    InStock = 180000,
                    IsAuthorized = true,
                    PartNumber = "595-LM358N",
                    MinOrderQty = "1",
                    LeadTime = "In Stock",
                    PriceBreaks = new List<PriceBreak>
                    {
                        new() { Quantity = 1, Price = 0.2400m, Currency = "USD" },
                        new() { Quantity = 10, Price = 0.1960m, Currency = "USD" },
                        new() { Quantity = 100, Price = 0.1530m, Currency = "USD" },
                        new() { Quantity = 1000, Price = 0.1100m, Currency = "USD" },
                        new() { Quantity = 5000, Price = 0.0970m, Currency = "USD" }
                    }
                },
                new()
                {
                    DistributorName = "element14",
                    DistributorUrl = "https://www.element14.com/community/search.jspa?q=LM358N",
                    InStock = 120000,
                    IsAuthorized = true,
                    PartNumber = "LM358N",
                    MinOrderQty = "1",
                    LeadTime = "In Stock",
                    PriceBreaks = new List<PriceBreak>
                    {
                        new() { Quantity = 1, Price = 0.2600m, Currency = "USD" },
                        new() { Quantity = 100, Price = 0.2000m, Currency = "USD" },
                        new() { Quantity = 1000, Price = 0.1450m, Currency = "USD" }
                    }
                }
            },
            Applications = new List<ApplicationScenario>
            {
                new() { Name = "Consumer Electronics", Icon = "monitor", Description = "Audio amplifiers, active filters in portable speakers, signal conditioning in home appliances, power management circuits." },
                new() { Name = "Industrial Control", Icon = "settings", Description = "General interfaces (e.g., temperature, pressure), motor control feedback loops, process control systems, data acquisition front-ends." },
                new() { Name = "Automotive", Icon = "car", Description = "Battery monitoring systems (non-critical), sensor signal amplification, lighting control." },
                new() { Name = "Medical Devices", Icon = "heart", Description = "Low-cost diagnostic equipment, portable patient monitoring (non-life-critical), signal amplification in laboratory instruments." },
                new() { Name = "IoT Devices", Icon = "wifi", Description = "Low power sensor nodes, smart home devices, environmental monitors, basic analog signal processing for connectivity modules." },
                new() { Name = "Power Supplies", Icon = "zap", Description = "Voltage regulation feedback, current sensing, overcurrent protection circuits, power supply monitoring." }
            },
            PriceTrend = new List<PriceTrendPoint>
            {
                new() { Month = "Mar 2025", Price = 0.1340m, Currency = "USD" },
                new() { Month = "Apr 2025", Price = 0.1342m, Currency = "USD" },
                new() { Month = "May 2025", Price = 0.1340m, Currency = "USD" },
                new() { Month = "Jun 2025", Price = 0.1337m, Currency = "USD" },
                new() { Month = "Jul 2025", Price = 0.1250m, Currency = "USD" },
                new() { Month = "Aug 2025", Price = 0.1340m, Currency = "USD" },
                new() { Month = "Sep 2025", Price = 0.1340m, Currency = "USD" },
                new() { Month = "Oct 2025", Price = 0.1345m, Currency = "USD" },
                new() { Month = "Nov 2025", Price = 0.1348m, Currency = "USD" },
                new() { Month = "Dec 2025", Price = 0.1348m, Currency = "USD" },
                new() { Month = "Jan 2026", Price = 0.1345m, Currency = "USD" },
                new() { Month = "Feb 2026", Price = 0.1345m, Currency = "USD" },
                new() { Month = "Mar 2026", Price = 0.1348m, Currency = "USD" }
            },
            Alternatives = new List<AlternativeComponent>
            {
                new() { Mpn = "MCP6002-I/P", ManufacturerName = "Microchip Technology", Description = "Dual Low Power, 1.8V to 5.5V Op Amp.", RelationType = "FunctionalEquivalent", PriceRange = "$0.30-$0.55", Notes = "Lower supply voltage range, rail-to-rail output, lower quiescent current, slightly higher gain-bandwidth product. More modern CMOS process." },
                new() { Mpn = "NJM2904D", ManufacturerName = "JRC (New Japan Radio)", Description = "Dual General Purpose Operational Amplifier.", RelationType = "PinCompatible", PriceRange = "$0.20-$0.40", Notes = "Direct pin-compatible replacement, similar electrical characteristics, often used as a direct alternative in many designs." },
                new() { Mpn = "OPA2340EA/250", ManufacturerName = "Texas Instruments", Description = "Rail-to-Rail, Low-Power, Single-Supply Op Amp.", RelationType = "Similar", PriceRange = "$1.20-$2.00", Notes = "Higher performance (lower offset, higher GBW, rail-to-rail VOL), but also higher cost and different package (SOIC-8). Suitable for upgrades where performance is critical." },
                new() { Mpn = "TL072CP", ManufacturerName = "Texas Instruments", Description = "Dual JFET-Input Operational Amplifier.", RelationType = "FunctionalEquivalent", PriceRange = "$0.40-$0.70", Notes = "JFET input provides very low input bias current, higher slew rate, and wider bandwidth. Suitable for audio and precision instrumentation where input impedance is critical; higher power consumption." }
            },
            News = new List<ComponentNews>
            {
                new() { Title = "Texas Instruments Unveils New Ultra-Low Power Op Amps for Extended Battery Life in IoT", Summary = "Texas Instruments recently launched a new series of operational amplifiers designed for ultra-low power consumption, targeting the growing market of battery-powered IoT devices.", Source = "Instrument Express", Tag = "Product Launch", PublishedAt = DateTime.UtcNow.AddDays(-7), Url = "#" },
                new() { Title = "Global Op-Amp Market Sees Steady Growth Driven by Industrial Automation and Automotive Electrification", Summary = "The operational amplifier market continues its steady expansion, fueled by increasing adoption in industrial automation and the rapid electrification of the automotive sector.", Source = "EE Times", Tag = "Market Trend", PublishedAt = DateTime.UtcNow.AddDays(-14), Url = "#" }
            }
        };

        // ─────────────────────────────────────────────────────────
        // NE555P 定时器
        // ─────────────────────────────────────────────────────────
        private static ComponentDetailDto BuildNe555pData() => new()
        {
            Mpn = "NE555P",
            ManufacturerName = "Texas Instruments",
            ShortDescription = "Single Precision Timer, 5V to 15V Supply",
            Description = "The NE555P is a highly stable device for generating accurate time delays or oscillation. Additional terminals are provided for triggering or resetting if desired. In the time delay mode of operation, the time is precisely controlled by one external resistor and capacitor.",
            LifecycleStatus = "Active",
            PackageType = "DIP-8",
            IsRoHSCompliant = true,
            DataSource = "Mock",
            FetchedAt = DateTime.UtcNow,
            Specs = new List<ComponentSpec>
            {
                new() { AttributeName = "Supply Voltage (Min)", DisplayValue = "4.5 V" },
                new() { AttributeName = "Supply Voltage (Max)", DisplayValue = "16 V" },
                new() { AttributeName = "Output Current (Max)", DisplayValue = "200 mA" },
                new() { AttributeName = "Timing Accuracy", DisplayValue = "1%" },
                new() { AttributeName = "Operating Temperature (Min)", DisplayValue = "0°C" },
                new() { AttributeName = "Operating Temperature (Max)", DisplayValue = "+70°C" },
                new() { AttributeName = "Frequency (Max)", DisplayValue = "500 kHz" }
            },
            Sellers = new List<DistributorPricing>
            {
                new()
                {
                    DistributorName = "DigiKey",
                    DistributorUrl = "https://www.digikey.com/en/products/filter/timer-ics/170?s=N4IgTCBcDaIIwFYBsAOAtHAzABjQJgHYBOAGhAF0BfIA",
                    InStock = 320000,
                    IsAuthorized = true,
                    PartNumber = "NE555P-ND",
                    MinOrderQty = "1",
                    LeadTime = "In Stock",
                    PriceBreaks = new List<PriceBreak>
                    {
                        new() { Quantity = 1, Price = 0.3200m, Currency = "USD" },
                        new() { Quantity = 10, Price = 0.2400m, Currency = "USD" },
                        new() { Quantity = 100, Price = 0.1800m, Currency = "USD" },
                        new() { Quantity = 1000, Price = 0.1400m, Currency = "USD" }
                    }
                },
                new()
                {
                    DistributorName = "Mouser",
                    DistributorUrl = "https://www.mouser.com/c/?q=NE555P",
                    InStock = 210000,
                    IsAuthorized = true,
                    PartNumber = "595-NE555P",
                    MinOrderQty = "1",
                    LeadTime = "In Stock",
                    PriceBreaks = new List<PriceBreak>
                    {
                        new() { Quantity = 1, Price = 0.3100m, Currency = "USD" },
                        new() { Quantity = 100, Price = 0.1900m, Currency = "USD" },
                        new() { Quantity = 1000, Price = 0.1500m, Currency = "USD" }
                    }
                }
            },
            Applications = new List<ApplicationScenario>
            {
                new() { Name = "Oscillator Circuits", Icon = "activity", Description = "Astable mode for generating square wave oscillations in clocks and tone generators." },
                new() { Name = "Pulse Generation", Icon = "zap", Description = "Monostable mode for producing precise time delays and single pulses." },
                new() { Name = "PWM Control", Icon = "sliders", Description = "Pulse width modulation for motor speed control and LED dimming." },
                new() { Name = "Sensor Interfaces", Icon = "cpu", Description = "Debouncing switches, converting sensor outputs to digital signals." }
            },
            PriceTrend = new List<PriceTrendPoint>
            {
                new() { Month = "Mar 2025", Price = 0.1800m, Currency = "USD" },
                new() { Month = "Jun 2025", Price = 0.1750m, Currency = "USD" },
                new() { Month = "Sep 2025", Price = 0.1780m, Currency = "USD" },
                new() { Month = "Dec 2025", Price = 0.1800m, Currency = "USD" },
                new() { Month = "Mar 2026", Price = 0.1820m, Currency = "USD" }
            },
            Alternatives = new List<AlternativeComponent>
            {
                new() { Mpn = "LM555CN", ManufacturerName = "Texas Instruments", Description = "Single Timer, Pin-Compatible Alternative.", RelationType = "PinCompatible", PriceRange = "$0.30-$0.50", Notes = "Direct pin-compatible replacement with similar performance characteristics." },
                new() { Mpn = "ICM7555IPA", ManufacturerName = "Renesas", Description = "CMOS Low Power Timer.", RelationType = "FunctionalEquivalent", PriceRange = "$0.50-$0.80", Notes = "CMOS version with much lower power consumption, suitable for battery-powered applications." }
            },
            News = new List<ComponentNews>
            {
                new() { Title = "Classic 555 Timer Still Going Strong in Modern Designs", Summary = "Despite being over 50 years old, the 555 timer continues to find applications in modern electronics, from IoT devices to educational kits.", Source = "Electronics Weekly", Tag = "Industry News", PublishedAt = DateTime.UtcNow.AddDays(-3), Url = "#" }
            }
        };

        // ─────────────────────────────────────────────────────────
        // STM32F103C8T6 微控制器
        // ─────────────────────────────────────────────────────────
        private static ComponentDetailDto BuildStm32Data() => new()
        {
            Mpn = "STM32F103C8T6",
            ManufacturerName = "STMicroelectronics",
            ShortDescription = "32-bit ARM Cortex-M3 MCU, 64KB Flash, 20KB RAM, 72MHz",
            Description = "The STM32F103C8T6 is a medium-density performance line ARM Cortex-M3 32-bit RISC core operating at a 72 MHz frequency, high-speed embedded memories (Flash memory up to 128 Kbytes and SRAM up to 20 Kbytes), and an extensive range of enhanced I/Os and peripherals connected to two APB buses.",
            LifecycleStatus = "Active",
            PackageType = "LQFP-48",
            IsRoHSCompliant = true,
            DataSource = "Mock",
            FetchedAt = DateTime.UtcNow,
            Specs = new List<ComponentSpec>
            {
                new() { AttributeName = "Core", DisplayValue = "ARM Cortex-M3" },
                new() { AttributeName = "CPU Speed (Max)", DisplayValue = "72 MHz" },
                new() { AttributeName = "Flash Memory", DisplayValue = "64 KB" },
                new() { AttributeName = "RAM", DisplayValue = "20 KB" },
                new() { AttributeName = "Supply Voltage (Min)", DisplayValue = "2.0 V" },
                new() { AttributeName = "Supply Voltage (Max)", DisplayValue = "3.6 V" },
                new() { AttributeName = "GPIO Pins", DisplayValue = "37" },
                new() { AttributeName = "ADC Channels", DisplayValue = "10 x 12-bit" },
                new() { AttributeName = "UART", DisplayValue = "3" },
                new() { AttributeName = "SPI", DisplayValue = "2" },
                new() { AttributeName = "I2C", DisplayValue = "2" },
                new() { AttributeName = "USB", DisplayValue = "USB 2.0 Full-Speed" },
                new() { AttributeName = "Operating Temperature", DisplayValue = "-40°C to +85°C" }
            },
            Sellers = new List<DistributorPricing>
            {
                new()
                {
                    DistributorName = "DigiKey",
                    DistributorUrl = "https://www.digikey.com/en/products/filter/microcontrollers/685?s=N4IgTCBcDaIIwFYBsAOAtHAzABjQJgHYBOAGhAF0BfIA",
                    InStock = 45000,
                    IsAuthorized = true,
                    PartNumber = "497-6063-ND",
                    MinOrderQty = "1",
                    LeadTime = "In Stock",
                    PriceBreaks = new List<PriceBreak>
                    {
                        new() { Quantity = 1, Price = 4.2000m, Currency = "USD" },
                        new() { Quantity = 10, Price = 3.8000m, Currency = "USD" },
                        new() { Quantity = 100, Price = 3.2000m, Currency = "USD" },
                        new() { Quantity = 1000, Price = 2.8000m, Currency = "USD" }
                    }
                },
                new()
                {
                    DistributorName = "Mouser",
                    DistributorUrl = "https://www.mouser.com/c/?q=STM32F103C8T6",
                    InStock = 32000,
                    IsAuthorized = true,
                    PartNumber = "511-STM32F103C8T6",
                    MinOrderQty = "1",
                    LeadTime = "In Stock",
                    PriceBreaks = new List<PriceBreak>
                    {
                        new() { Quantity = 1, Price = 4.3500m, Currency = "USD" },
                        new() { Quantity = 10, Price = 3.9000m, Currency = "USD" },
                        new() { Quantity = 100, Price = 3.3000m, Currency = "USD" }
                    }
                }
            },
            Applications = new List<ApplicationScenario>
            {
                new() { Name = "Embedded Systems", Icon = "cpu", Description = "General-purpose embedded control for industrial and consumer applications." },
                new() { Name = "Motor Control", Icon = "settings", Description = "Brushless DC motor control with advanced timer peripherals." },
                new() { Name = "IoT Gateway", Icon = "wifi", Description = "Edge computing and sensor aggregation for IoT networks." },
                new() { Name = "USB Applications", Icon = "usb", Description = "USB HID, CDC, and custom USB device implementations." }
            },
            PriceTrend = new List<PriceTrendPoint>
            {
                new() { Month = "Mar 2025", Price = 3.8000m, Currency = "USD" },
                new() { Month = "Jun 2025", Price = 3.9500m, Currency = "USD" },
                new() { Month = "Sep 2025", Price = 4.1000m, Currency = "USD" },
                new() { Month = "Dec 2025", Price = 4.2000m, Currency = "USD" },
                new() { Month = "Mar 2026", Price = 4.2000m, Currency = "USD" }
            },
            Alternatives = new List<AlternativeComponent>
            {
                new() { Mpn = "STM32F103CBT6", ManufacturerName = "STMicroelectronics", Description = "Same core, 128KB Flash version.", RelationType = "Similar", PriceRange = "$4.50-$5.50", Notes = "Upgrade option with double the flash memory." },
                new() { Mpn = "GD32F103C8T6", ManufacturerName = "GigaDevice", Description = "Pin-compatible STM32F103 clone.", RelationType = "PinCompatible", PriceRange = "$1.50-$2.50", Notes = "Lower cost alternative, pin and software compatible for most use cases." }
            },
            News = new List<ComponentNews>
            {
                new() { Title = "STM32 Supply Chain Normalizes After 2024 Shortage", Summary = "STMicroelectronics reports improved availability of STM32F1 series parts as production capacity increases.", Source = "EE Times", Tag = "Supply Chain", PublishedAt = DateTime.UtcNow.AddDays(-5), Url = "#" }
            }
        };

        // ─────────────────────────────────────────────────────────
        // 通用模板（未知型号）
        // ─────────────────────────────────────────────────────────
        private static ComponentDetailDto BuildGenericData(string mpn) => new()
        {
            Mpn = mpn.ToUpperInvariant(),
            ManufacturerName = "Unknown Manufacturer",
            ShortDescription = $"Electronic Component - {mpn.ToUpperInvariant()}",
            Description = $"This is mock data for {mpn.ToUpperInvariant()}. Connect to Nexar API for real specifications.",
            LifecycleStatus = "Unknown",
            PackageType = "Unknown",
            IsRoHSCompliant = null,
            DataSource = "Mock",
            FetchedAt = DateTime.UtcNow,
            Specs = new List<ComponentSpec>
            {
                new() { AttributeName = "Status", DisplayValue = "Mock Data - Real API Not Connected" },
                new() { AttributeName = "MPN", DisplayValue = mpn.ToUpperInvariant() }
            },
            Sellers = new List<DistributorPricing>(),
            Applications = new List<ApplicationScenario>(),
            PriceTrend = new List<PriceTrendPoint>(),
            Alternatives = new List<AlternativeComponent>(),
            News = new List<ComponentNews>()
        };
    }
}
