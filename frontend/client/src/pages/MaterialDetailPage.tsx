/**
 * MaterialDetailPage - 电子元器件物料详情页
 * Design: Dark Tech Theme
 * Colors: bg #0d1117, card #131d2e, accent #00d4ff, text #e0e0e0
 * Layout: Full-width dark page, two-column sections
 */
import { useState } from "react";
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer
} from "recharts";
import {
  ChevronDown, ChevronUp, ExternalLink, Download, X,
  Cpu, Zap, Car, Heart, Wifi, Battery, FileText, Tag,
  TrendingUp, Newspaper, Package, AlertCircle, Mail
} from "lucide-react";

// ─── Mock Data ────────────────────────────────────────────────────────────────
const component = {
  partNumber: "LM358N",
  status: "Active",
  category: "Industrial",
  description: "Dual Operational Amplifier, Low Power, General Purpose",
  manufacturer: "Texas Instruments",
  packageType: "DIP-8",
  rohs: "RoHS",
  datasheet: "Datasheet",
};

const gradeInfo = {
  operatingTemp: "-40°C to +85°C",
  certifications: ["RoHS Compliant"],
  description:
    "The LM358N is primarily designed for commercial and light industrial applications. Its temperature range of -40°C to +85°C makes it suitable for environments where moderate temperature variations are expected. It offers reliable performance for general-purpose signal conditioning but is not typically qualified for stringent automotive or military standards requiring extended temperature ranges or enhanced reliability.",
};

const specifications = [
  { name: "Number of Channels", value: "2" },
  { name: "Supply Voltage (Min)", value: "3 V" },
  { name: "Supply Voltage (Max)", value: "32 V" },
  { name: "Gain Bandwidth Product", value: "0.7 MHz" },
  { name: "Slew Rate", value: "0.3 V/μs" },
  { name: "Input Offset Voltage (Max)", value: "7 mV" },
  { name: "Input Bias Current (Max)", value: "250 nA" },
  { name: "Quiescent Current", value: "0.5 mA" },
  { name: "CMRR (Min)", value: "65 dB" },
  { name: "Output Current (Max)", value: "40 mA" },
];

const distributors = [
  {
    name: "DigiKey",
    authorized: true,
    partNo: "296-LM358NE4-ND",
    pkg: "Tube",
    moq: "1",
    rohs: "RoHS",
    stock: 250000,
    prices: [
      { qty: "1+", price: "$0.2500" },
      { qty: "100+", price: "$0.1800" },
      { qty: "1,000+", price: "$0.1200" },
      { qty: "10,000+", price: "$0.1000" },
    ],
  },
  {
    name: "Mouser",
    authorized: true,
    partNo: "595-LM358N",
    pkg: "Tube",
    moq: "1",
    rohs: "RoHS",
    stock: 180000,
    prices: [
      { qty: "1+", price: "$0.2400" },
      { qty: "100+", price: "$0.1900" },
      { qty: "1,000+", price: "$0.1300" },
      { qty: "5,000+", price: "$0.1100" },
    ],
  },
  {
    name: "element14",
    authorized: true,
    partNo: "1-LM358N",
    pkg: "Tube",
    moq: "2",
    rohs: "RoHS",
    stock: 120000,
    prices: [
      { qty: "1+", price: "$0.2200" },
      { qty: "100+", price: "$0.2000" },
      { qty: "1,000+", price: "$0.1650" },
    ],
  },
];

const applications = [
  {
    icon: Cpu,
    title: "Consumer Electronics",
    desc: "Audio amplifiers, active filters in portable speakers, signal conditioning in home appliances, power management circuits.",
  },
  {
    icon: Zap,
    title: "Industrial Control",
    desc: "General interfaces (e.g., temperature, pressure), motor control feedback loops, process control systems, data acquisition front-ends.",
  },
  {
    icon: Car,
    title: "Automotive",
    desc: "Battery monitoring systems (non-critical), sensor signal conditioning, linear sensor signal amplification, lighting control.",
  },
  {
    icon: Heart,
    title: "Medical Devices",
    desc: "Low-critical diagnostic equipment, portable patient monitoring (non-life-critical), signal amplification in laboratory instruments.",
  },
  {
    icon: Wifi,
    title: "IoT Devices",
    desc: "Low power sensor nodes, smart home devices, environmental monitors, basic analog signal processing for connectivity modules.",
  },
  {
    icon: Battery,
    title: "Power Supplies",
    desc: "Voltage regulation feedback, current sensing, overcurrent protection circuits, power supply monitoring.",
  },
];

const priceHistory = [
  { month: "Mar'25", price: 0.143 },
  { month: "Apr'25", price: 0.143 },
  { month: "May'25", price: 0.143 },
  { month: "Jun'25", price: 0.143 },
  { month: "Jul'25", price: 0.137 },
  { month: "Aug'25", price: 0.137 },
  { month: "Sep'25", price: 0.143 },
  { month: "Oct'25", price: 0.143 },
  { month: "Nov'25", price: 0.145 },
  { month: "Dec'25", price: 0.145 },
  { month: "Jan'26", price: 0.145 },
  { month: "Feb'26", price: 0.145 },
];

const news = [
  {
    tag: "Product Launch",
    tagColor: "#00d4ff",
    date: "Feb 2025",
    title: "Texas Instruments Unveils New Ultra-Low Power Op Amps for Extended Battery Life in IoT",
    excerpt:
      "Texas Instruments recently launched a new series of operational amplifiers designed for ultra-low power consumption, targeting the growing market of battery-powered IoT devices.",
    source: "Instrument Express",
  },
  {
    tag: "Market Trend",
    tagColor: "#ffd600",
    date: "Jan 2025",
    title: "Global Op-Amp Market Sees Steady Growth Driven by Industrial Automation and Automotive Electrification",
    excerpt:
      "The operational amplifier market continues its steady expansion, fueled by increasing adoption in industrial automation and the rapid electrification of the automotive sector.",
    source: "Scan Now",
  },
];

const alternatives = [
  {
    partNo: "MCP6002-I/P",
    tag: "Functional Equivalent",
    tagColor: "#00d4ff",
    manufacturer: "Microchip Technology",
    desc: "Dual, Low Power, 1.8V to 5.5V Op Amp.",
    notes: "Lower supply voltage range, rail-to-rail output, lower quiescent current, slightly higher gain-bandwidth product. More modern CMOS process.",
    priceRange: "$0.30–$0.55",
  },
  {
    partNo: "NJM2904D",
    tag: "Pin Compatible",
    tagColor: "#00e676",
    manufacturer: "JRC (New Japan Radio)",
    desc: "Dual General Purpose Operational Amplifier.",
    notes: "Direct pin-compatible replacement, similar electrical characteristics, often used as a direct alternative in many designs.",
    priceRange: "$0.20–$0.40",
  },
  {
    partNo: "OPA2340EA/250",
    tag: "Similar",
    tagColor: "#ffd600",
    manufacturer: "Texas Instruments",
    desc: "Rail-to-Rail, Low-Power, Single-Supply Op Amp.",
    notes: "Higher performance (lower offset, higher GBW, rail-to-rail VO), but also higher cost and different package (SOIC-8). Suitable for upgrades where performance is critical.",
    priceRange: "$1.20–$2.00",
  },
  {
    partNo: "TL072CP",
    tag: "Functional Equivalent",
    tagColor: "#00d4ff",
    manufacturer: "Texas Instruments",
    desc: "Dual JFET-Input Operational Amplifier.",
    notes: "JFET input provides very low input bias current, higher slew rate, and wider bandwidth. Suitable for audio and precision instrumentation where input impedance is critical. Higher power consumption.",
    priceRange: "$0.40–$0.70",
  },
];

// ─── Sub-Components ───────────────────────────────────────────────────────────

function SectionHeader({ icon: Icon, title, subtitle }: { icon: any; title: string; subtitle?: string }) {
  return (
    <div className="flex items-center gap-2 mb-4">
      <Icon size={16} className="text-[#00d4ff]" />
      <span className="text-[#00d4ff] text-sm font-semibold tracking-wide">{title}</span>
      {subtitle && <span className="text-[#8899aa] text-xs ml-1">{subtitle}</span>}
    </div>
  );
}

function StatusBadge({ label, color }: { label: string; color: string }) {
  return (
    <span
      className="px-2 py-0.5 rounded text-xs font-semibold border"
      style={{ color, borderColor: color, backgroundColor: `${color}18` }}
    >
      {label}
    </span>
  );
}

function CustomTooltip({ active, payload, label }: any) {
  if (active && payload && payload.length) {
    return (
      <div className="bg-[#1a2535] border border-[#1e3a5f] rounded px-3 py-2">
        <p className="text-[#8899aa] text-xs">{label}</p>
        <p className="text-[#00d4ff] text-sm font-mono font-semibold">${payload[0].value.toFixed(3)}</p>
      </div>
    );
  }
  return null;
}

// ─── Main Component ───────────────────────────────────────────────────────────
export default function MaterialDetailPage() {
  const [showAllSpecs, setShowAllSpecs] = useState(false);
  const [showAllDistributors, setShowAllDistributors] = useState(false);
  const [expandedAlt, setExpandedAlt] = useState<number | null>(null);
  const [showEmailCta, setShowEmailCta] = useState(true);

  const visibleSpecs = showAllSpecs ? specifications : specifications.slice(0, 6);
  const visibleDistributors = showAllDistributors ? distributors : distributors.slice(0, 3);

  return (
    <div className="min-h-screen bg-[#0d1117] text-[#e0e0e0] font-sans">
      {/* ── Top Bar ─────────────────────────────────────────────────────────── */}
      <div className="border-b border-[#1e3a5f] bg-[#0d1117] sticky top-0 z-50">
        <div className="max-w-7xl mx-auto px-6 py-3 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <span className="text-[#8899aa] text-sm">Search Results</span>
            <span className="text-[#8899aa] text-sm">for</span>
            <span className="text-[#00d4ff] text-sm font-mono font-semibold">'lm358'</span>
          </div>
          <div className="flex items-center gap-3">
            <button className="flex items-center gap-2 px-3 py-1.5 rounded border border-[#1e3a5f] text-[#00d4ff] text-xs hover:bg-[#131d2e] transition-colors">
              <Download size={12} />
              Export PDF
            </button>
            <button className="text-[#8899aa] text-xs hover:text-[#e0e0e0] transition-colors">
              Clear Results
            </button>
          </div>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-6 py-6 space-y-6">

        {/* ── Component Header ─────────────────────────────────────────────── */}
        <div className="bg-[#131d2e] border border-[#1e3a5f] rounded-lg p-5">
          <div className="flex items-center gap-3 mb-2">
            <h1 className="text-2xl font-bold text-white font-mono tracking-wider">
              {component.partNumber}
            </h1>
            <StatusBadge label={component.status} color="#00e676" />
            <StatusBadge label={component.category} color="#00d4ff" />
          </div>
          <p className="text-[#8899aa] text-sm mb-3">{component.description}</p>
          <div className="flex flex-wrap gap-2">
            {[component.manufacturer, "Operational Amplifiers (Op-Amps)", component.packageType, component.rohs, component.datasheet].map((tag, i) => (
              <span
                key={i}
                className="flex items-center gap-1.5 px-2.5 py-1 rounded border border-[#243b55] text-[#8899aa] text-xs hover:border-[#00d4ff] hover:text-[#00d4ff] cursor-pointer transition-colors"
              >
                {i === 4 && <FileText size={10} />}
                {i === 3 && <Tag size={10} />}
                {tag}
              </span>
            ))}
          </div>
        </div>

        {/* ── Grade Classification ─────────────────────────────────────────── */}
        <div className="bg-[#131d2e] border border-[#1e3a5f] rounded-lg p-5">
          <SectionHeader icon={AlertCircle} title="Grade Classification" />
          <div className="grid grid-cols-3 gap-4">
            <div className="bg-[#0d1117] border border-[#1e3a5f] rounded p-4">
              <p className="text-[#8899aa] text-xs mb-2">Operating Temperature</p>
              <p className="text-white text-sm font-mono">{gradeInfo.operatingTemp}</p>
            </div>
            <div className="bg-[#0d1117] border border-[#1e3a5f] rounded p-4">
              <p className="text-[#8899aa] text-xs mb-2">Certifications</p>
              {gradeInfo.certifications.map((c, i) => (
                <span key={i} className="inline-block px-2 py-0.5 rounded text-xs font-semibold bg-[#00e67620] text-[#00e676] border border-[#00e67640]">
                  {c}
                </span>
              ))}
            </div>
            <div className="bg-[#0d1117] border border-[#1e3a5f] rounded p-4">
              <p className="text-[#8899aa] text-xs mb-2">Description</p>
              <p className="text-[#cdd6e0] text-xs leading-relaxed line-clamp-5">{gradeInfo.description}</p>
            </div>
          </div>
        </div>

        {/* ── Specs + Pricing ──────────────────────────────────────────────── */}
        <div className="grid grid-cols-2 gap-6">

          {/* Specifications */}
          <div className="bg-[#131d2e] border border-[#1e3a5f] rounded-lg p-5">
            <SectionHeader icon={Cpu} title="Specifications" />
            <div className="space-y-0">
              {visibleSpecs.map((spec, i) => (
                <div
                  key={i}
                  className="flex items-center justify-between py-2 border-b border-[#1e3a5f] last:border-0"
                >
                  <span className="text-[#8899aa] text-xs">{spec.name}</span>
                  <span className="text-[#e0e0e0] text-xs font-mono font-semibold">{spec.value}</span>
                </div>
              ))}
            </div>
            <button
              onClick={() => setShowAllSpecs(!showAllSpecs)}
              className="mt-3 flex items-center gap-1 text-[#00d4ff] text-xs hover:text-[#00b8d4] transition-colors"
            >
              {showAllSpecs ? <ChevronUp size={12} /> : <ChevronDown size={12} />}
              {showAllSpecs ? "Show Less" : `Show All (${specifications.length})`}
            </button>
          </div>

          {/* Distributor Pricing */}
          <div className="bg-[#131d2e] border border-[#1e3a5f] rounded-lg p-5">
            <div className="flex items-center justify-between mb-4">
              <SectionHeader icon={Tag} title="Distributor Pricing" subtitle="@digikey.com.sg" />
            </div>
            <div className="space-y-4">
              {visibleDistributors.map((dist, i) => (
                <div key={i} className="bg-[#0d1117] border border-[#1e3a5f] rounded-lg p-4">
                  <div className="flex items-center justify-between mb-3">
                    <div className="flex items-center gap-2">
                      <span className="text-white text-sm font-semibold">{dist.name}</span>
                      <span className="px-1.5 py-0.5 rounded text-[10px] font-semibold bg-[#00d4ff20] text-[#00d4ff] border border-[#00d4ff40]">
                        Authorized
                      </span>
                    </div>
                    <span className="text-[#00e676] text-xs font-mono">
                      {dist.stock.toLocaleString()} in stock
                    </span>
                  </div>
                  <div className="text-[#8899aa] text-[10px] mb-2 font-mono">
                    {dist.partNo} · {dist.pkg} MOQ {dist.moq} · {dist.rohs}
                  </div>
                  <div className="grid grid-cols-2 gap-x-4">
                    <div className="text-[#8899aa] text-[10px] mb-1">Qty</div>
                    <div className="text-[#8899aa] text-[10px] mb-1">Unit Price (USD)</div>
                    {dist.prices.map((p, j) => (
                      <>
                        <div key={`qty-${j}`} className="text-[#cdd6e0] text-xs font-mono py-0.5">{p.qty}</div>
                        <div key={`price-${j}`} className="text-[#00d4ff] text-xs font-mono font-semibold py-0.5">{p.price}</div>
                      </>
                    ))}
                  </div>
                </div>
              ))}
            </div>
            {distributors.length > 3 && (
              <button
                onClick={() => setShowAllDistributors(!showAllDistributors)}
                className="mt-3 flex items-center gap-1 text-[#00d4ff] text-xs hover:text-[#00b8d4] transition-colors"
              >
                {showAllDistributors ? <ChevronUp size={12} /> : <ChevronDown size={12} />}
                {showAllDistributors ? "Show Less" : `Show All Distributors (${distributors.length})`}
              </button>
            )}
          </div>
        </div>

        {/* ── Applications + Price Trend ───────────────────────────────────── */}
        <div className="grid grid-cols-2 gap-6">

          {/* Industry Applications */}
          <div className="bg-[#131d2e] border border-[#1e3a5f] rounded-lg p-5">
            <SectionHeader icon={Zap} title="Industry Applications" />
            <div className="grid grid-cols-2 gap-3">
              {applications.map((app, i) => (
                <div key={i} className="bg-[#0d1117] border border-[#1e3a5f] rounded-lg p-3 hover:border-[#00d4ff40] transition-colors">
                  <div className="flex items-center gap-2 mb-2">
                    <div className="w-7 h-7 rounded bg-[#00d4ff15] flex items-center justify-center">
                      <app.icon size={14} className="text-[#00d4ff]" />
                    </div>
                    <span className="text-[#e0e0e0] text-xs font-semibold">{app.title}</span>
                  </div>
                  <p className="text-[#8899aa] text-[10px] leading-relaxed">{app.desc}</p>
                </div>
              ))}
            </div>
          </div>

          {/* 12-Month Price Trend */}
          <div className="bg-[#131d2e] border border-[#1e3a5f] rounded-lg p-5">
            <div className="flex items-center justify-between mb-1">
              <SectionHeader icon={TrendingUp} title="12-Month Price Trend" subtitle="DigiKey @ 1000 U/E" />
            </div>
            <p className="text-[#8899aa] text-[10px] mb-4 leading-relaxed">
              The price for the LM358N has remained remarkably stable over the past 12 months, with only minor fluctuations, indicating a mature and consistent market.
            </p>
            <ResponsiveContainer width="100%" height={200}>
              <LineChart data={priceHistory} margin={{ top: 5, right: 10, left: 0, bottom: 5 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#1e3a5f" />
                <XAxis
                  dataKey="month"
                  tick={{ fill: "#8899aa", fontSize: 9 }}
                  axisLine={{ stroke: "#1e3a5f" }}
                  tickLine={false}
                />
                <YAxis
                  tick={{ fill: "#8899aa", fontSize: 9 }}
                  axisLine={{ stroke: "#1e3a5f" }}
                  tickLine={false}
                  tickFormatter={(v) => `$${v.toFixed(2)}`}
                  domain={[0.12, 0.16]}
                  width={40}
                />
                <Tooltip content={<CustomTooltip />} />
                <Line
                  type="monotone"
                  dataKey="price"
                  stroke="#00d4ff"
                  strokeWidth={2}
                  dot={{ fill: "#00d4ff", r: 3, strokeWidth: 0 }}
                  activeDot={{ r: 5, fill: "#00d4ff" }}
                />
              </LineChart>
            </ResponsiveContainer>
            {/* Price history list */}
            <div className="mt-3 flex flex-wrap gap-x-4 gap-y-1">
              {priceHistory.map((p, i) => (
                <span key={i} className="text-[#8899aa] text-[9px] font-mono">
                  → {p.month}: <span className="text-[#cdd6e0]">${p.price.toFixed(3)}</span>
                </span>
              ))}
            </div>
          </div>
        </div>

        {/* ── Related News ─────────────────────────────────────────────────── */}
        <div className="bg-[#131d2e] border border-[#1e3a5f] rounded-lg p-5">
          <SectionHeader
            icon={Newspaper}
            title="Related News"
            subtitle="(Texas Instruments / Operational Amplifiers (Op-Amps))"
          />
          <div className="grid grid-cols-2 gap-4">
            {news.map((item, i) => (
              <div key={i} className="bg-[#0d1117] border border-[#1e3a5f] rounded-lg p-4 hover:border-[#00d4ff40] transition-colors cursor-pointer group">
                <div className="flex items-center justify-between mb-2">
                  <span
                    className="px-2 py-0.5 rounded text-[10px] font-semibold border"
                    style={{
                      color: item.tagColor,
                      borderColor: `${item.tagColor}60`,
                      backgroundColor: `${item.tagColor}15`,
                    }}
                  >
                    {item.tag}
                  </span>
                  <span className="text-[#8899aa] text-[10px]">{item.date}</span>
                </div>
                <h3 className="text-[#e0e0e0] text-sm font-semibold mb-2 leading-snug group-hover:text-[#00d4ff] transition-colors">
                  {item.title}
                </h3>
                <p className="text-[#8899aa] text-[10px] leading-relaxed mb-3">{item.excerpt}</p>
                <div className="flex items-center gap-1 text-[#00d4ff] text-[10px]">
                  <ExternalLink size={10} />
                  <span>{item.source}</span>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* ── Alternative Components ───────────────────────────────────────── */}
        <div className="bg-[#131d2e] border border-[#1e3a5f] rounded-lg p-5">
          <div className="flex items-center justify-between mb-4">
            <SectionHeader icon={Package} title="Alternative Components" subtitle={`(${alternatives.length} found)`} />
          </div>
          <div className="space-y-2">
            {alternatives.map((alt, i) => (
              <div
                key={i}
                className="bg-[#0d1117] border border-[#1e3a5f] rounded-lg overflow-hidden hover:border-[#243b55] transition-colors"
              >
                <div
                  className="flex items-center justify-between px-4 py-3 cursor-pointer"
                  onClick={() => setExpandedAlt(expandedAlt === i ? null : i)}
                >
                  <div className="flex items-center gap-3">
                    <span className="text-[#00d4ff] text-sm font-mono font-bold">{alt.partNo}</span>
                    <span
                      className="px-2 py-0.5 rounded text-[10px] font-semibold border"
                      style={{
                        color: alt.tagColor,
                        borderColor: `${alt.tagColor}60`,
                        backgroundColor: `${alt.tagColor}15`,
                      }}
                    >
                      {alt.tag}
                    </span>
                  </div>
                  <div className="flex items-center gap-4">
                    <span className="text-[#00d4ff] text-xs font-mono font-semibold">{alt.priceRange}</span>
                    {expandedAlt === i ? (
                      <ChevronUp size={14} className="text-[#8899aa]" />
                    ) : (
                      <ChevronDown size={14} className="text-[#8899aa]" />
                    )}
                  </div>
                </div>
                {expandedAlt === i && (
                  <div className="px-4 pb-4 border-t border-[#1e3a5f]">
                    <p className="text-[#8899aa] text-xs mt-3 mb-1">{alt.manufacturer}</p>
                    <p className="text-[#cdd6e0] text-xs mb-2">{alt.desc}</p>
                    <div className="flex items-start gap-2">
                      <span className="text-[#8899aa] text-[10px] mt-0.5">◎</span>
                      <p className="text-[#8899aa] text-[10px] leading-relaxed">{alt.notes}</p>
                    </div>
                  </div>
                )}
                {expandedAlt !== i && (
                  <div className="px-4 pb-3">
                    <p className="text-[#8899aa] text-[10px]">{alt.manufacturer}</p>
                    <p className="text-[#cdd6e0] text-[10px]">{alt.desc}</p>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>

        {/* ── Tip ──────────────────────────────────────────────────────────── */}
        <div className="bg-[#00d4ff10] border border-[#00d4ff30] rounded-lg p-4 flex items-start gap-3">
          <div className="w-6 h-6 rounded bg-[#00d4ff20] flex items-center justify-center flex-shrink-0 mt-0.5">
            <AlertCircle size={12} className="text-[#00d4ff]" />
          </div>
          <div>
            <span className="text-[#00d4ff] text-xs font-semibold">Tip: </span>
            <span className="text-[#cdd6e0] text-xs leading-relaxed">
              The LM358N is a very common and versatile dual-operational amplifier. Consider its functional equivalents like MCP6002-I/P for lower voltage/power applications or TL072CP for applications requiring very low input bias current. For higher performance, the OPA2340EA/250 offers significant improvements at a higher cost.
            </span>
          </div>
        </div>

        {/* ── Email CTA ────────────────────────────────────────────────────── */}
        {showEmailCta && (
          <div className="bg-[#131d2e] border border-[#1e3a5f] rounded-lg px-5 py-4 flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded bg-[#00d4ff20] flex items-center justify-center">
                <Mail size={14} className="text-[#00d4ff]" />
              </div>
              <div>
                <p className="text-[#e0e0e0] text-sm font-semibold">Would you like to receive these search results?</p>
                <p className="text-[#8899aa] text-xs">We'll send a detailed report to your email address.</p>
              </div>
            </div>
            <div className="flex items-center gap-3">
              <button
                onClick={() => setShowEmailCta(false)}
                className="px-4 py-2 rounded border border-[#1e3a5f] text-[#8899aa] text-xs hover:text-[#e0e0e0] hover:border-[#243b55] transition-colors"
              >
                No, thanks
              </button>
              <button className="px-4 py-2 rounded bg-[#00d4ff] text-[#0d1117] text-xs font-semibold hover:bg-[#00b8d4] transition-colors">
                Yes, send me
              </button>
              <button onClick={() => setShowEmailCta(false)} className="text-[#8899aa] hover:text-[#e0e0e0] transition-colors">
                <X size={14} />
              </button>
            </div>
          </div>
        )}

        <div className="h-6" />
      </div>
    </div>
  );
}
