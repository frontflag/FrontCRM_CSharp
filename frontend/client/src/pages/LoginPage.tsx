/**
 * LoginPage — FrontCRM Deep Quantum Theme
 * Full-screen dark navy with circuit board background
 * Glassmorphism login card, cyan glow effects
 */
import { useState } from "react";
import { useLocation } from "wouter";
import { Eye, EyeOff, Zap, Lock, User } from "lucide-react";

const LOGIN_BG = "https://d2xsxph8kpxj0f.cloudfront.net/310519663314032098/3MtojPhFHKzqgDYT9n9s2m/login-bg-ZYkKmSTzau6ZsjBg6uRUti.webp";

export default function LoginPage() {
  const [, navigate] = useLocation();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [showPwd, setShowPwd] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!username || !password) {
      setError("请输入用户名和密码");
      return;
    }
    setLoading(true);
    setError("");
    await new Promise((r) => setTimeout(r, 1200));
    setLoading(false);
    navigate("/dashboard");
  };

  return (
    <div
      className="min-h-screen flex items-center justify-center relative overflow-hidden"
      style={{ background: "#192A3F" }}
    >
      {/* Background image */}
      <div
        className="absolute inset-0 bg-cover bg-center"
        style={{
          backgroundImage: `url(${LOGIN_BG})`,
          opacity: 0.6,
        }}
      />

      {/* Grid overlay */}
      <div
        className="absolute inset-0"
        style={{
          backgroundImage:
            "linear-gradient(rgba(0,212,255,0.04) 1px, transparent 1px), linear-gradient(90deg, rgba(0,212,255,0.04) 1px, transparent 1px)",
          backgroundSize: "40px 40px",
        }}
      />

      {/* Gradient overlay */}
      <div
        className="absolute inset-0"
        style={{
          background:
            "radial-gradient(ellipse at center, rgba(0,102,255,0.08) 0%, #192A3F 70%)",
        }}
      />

      {/* Login card */}
      <div
        className="relative z-10 w-full max-w-md mx-4 animate-fade-in-up"
        style={{
          background: "#0A1628",
          backdropFilter: "blur(20px)",
          border: "1px solid rgba(0, 212, 255, 0.2)",
          borderRadius: "12px",
          boxShadow:
            "0 0 60px rgba(0, 212, 255, 0.08), 0 0 120px rgba(0, 102, 255, 0.05), inset 0 1px 0 rgba(0, 212, 255, 0.15)",
        }}
      >
        {/* Card header */}
        <div
          className="px-10 pt-10 pb-8 text-center"
          style={{ borderBottom: "1px solid rgba(0,212,255,0.1)" }}
        >
          {/* Logo */}
          <div className="flex justify-center mb-5">
            <div
              className="w-16 h-16 rounded-xl flex items-center justify-center"
              style={{
                background: "linear-gradient(135deg, rgba(0,212,255,0.15), rgba(0,102,255,0.15))",
                border: "1px solid rgba(0,212,255,0.35)",
                boxShadow: "0 0 30px rgba(0,212,255,0.2)",
              }}
            >
              <Zap size={32} style={{ color: "#00D4FF" }} />
            </div>
          </div>

          <h1
            className="text-2xl font-bold tracking-widest mb-1"
            style={{ fontFamily: "Orbitron, sans-serif", color: "#00D4FF" }}
          >
            FrontCRM
          </h1>
          <p className="text-sm" style={{ color: "rgba(0,212,255,0.5)" }}>
            智能进销存管理系统
          </p>
        </div>

        {/* Form */}
        <form onSubmit={handleLogin} className="px-10 py-8">
          <div className="space-y-5">
            {/* Username */}
            <div>
              <label
                className="block text-xs font-medium mb-2 tracking-wider"
                style={{ color: "rgba(0,212,255,0.7)", fontFamily: "Noto Sans SC, sans-serif" }}
              >
                用户名
              </label>
              <div className="relative">
                <User
                  size={15}
                  className="absolute left-3 top-1/2 -translate-y-1/2"
                  style={{ color: "rgba(200,216,232,0.5)" }}
                />
                <input
                  type="text"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  placeholder="请输入用户名"
                  className="w-full pl-9 pr-4 py-3 text-sm rounded outline-none transition-all"
                  style={{
                    background: "#162233",
                    border: "1px solid rgba(0,212,255,0.2)",
                    color: "#E0F4FF",
                    fontFamily: "Noto Sans SC, sans-serif",
                  }}
                  onFocus={(e) => {
                    e.target.style.borderColor = "rgba(0,212,255,0.5)";
                    e.target.style.boxShadow = "0 0 15px rgba(0,212,255,0.1)";
                  }}
                  onBlur={(e) => {
                    e.target.style.borderColor = "rgba(0,212,255,0.2)";
                    e.target.style.boxShadow = "none";
                  }}
                />
              </div>
            </div>

            {/* Password */}
            <div>
              <label
                className="block text-xs font-medium mb-2 tracking-wider"
                style={{ color: "rgba(0,212,255,0.7)", fontFamily: "Noto Sans SC, sans-serif" }}
              >
                密码
              </label>
              <div className="relative">
                <Lock
                  size={15}
                  className="absolute left-3 top-1/2 -translate-y-1/2"
                  style={{ color: "rgba(200,216,232,0.5)" }}
                />
                <input
                  type={showPwd ? "text" : "password"}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="请输入密码"
                  className="w-full pl-9 pr-10 py-3 text-sm rounded outline-none transition-all"
                  style={{
                    background: "#162233",
                    border: "1px solid rgba(0,212,255,0.2)",
                    color: "#E0F4FF",
                    fontFamily: "Noto Sans SC, sans-serif",
                  }}
                  onFocus={(e) => {
                    e.target.style.borderColor = "rgba(0,212,255,0.5)";
                    e.target.style.boxShadow = "0 0 15px rgba(0,212,255,0.1)";
                  }}
                  onBlur={(e) => {
                    e.target.style.borderColor = "rgba(0,212,255,0.2)";
                    e.target.style.boxShadow = "none";
                  }}
                />
                <button
                  type="button"
                  className="absolute right-3 top-1/2 -translate-y-1/2"
                  style={{ color: "rgba(200,216,232,0.5)" }}
                  onClick={() => setShowPwd(!showPwd)}
                >
                  {showPwd ? <EyeOff size={15} /> : <Eye size={15} />}
                </button>
              </div>
            </div>

            {/* Error */}
            {error && (
              <div
                className="text-xs px-3 py-2 rounded"
                style={{
                  background: "rgba(255,107,53,0.1)",
                  border: "1px solid rgba(255,255,255,0.10)",
                  color: "#FF6B35",
                }}
              >
                {error}
              </div>
            )}

            {/* Options */}
            <div className="flex items-center justify-between">
              <label className="flex items-center gap-2 text-xs cursor-pointer" style={{ color: "rgba(0,212,255,0.5)" }}>
                <input type="checkbox" className="w-3 h-3 accent-cyan-400" />
                记住我
              </label>
              <button type="button" className="text-xs" style={{ color: "rgba(0,212,255,0.5)" }}>
                忘记密码？
              </button>
            </div>

            {/* Submit */}
            <button
              type="submit"
              disabled={loading}
              className="w-full py-3 rounded text-sm font-semibold tracking-wider transition-all"
              style={{
                background: loading
                  ? "rgba(0,212,255,0.1)"
                  : "linear-gradient(135deg, rgba(0,212,255,0.25), rgba(0,102,255,0.25))",
                border: "1px solid rgba(0,212,255,0.4)",
                color: loading ? "rgba(0,212,255,0.4)" : "#00D4FF",
                boxShadow: loading ? "none" : "0 0 20px rgba(0,212,255,0.2)",
                fontFamily: "Orbitron, sans-serif",
                letterSpacing: "0.15em",
              }}
            >
              {loading ? (
                <span className="flex items-center justify-center gap-2">
                  <span
                    className="w-4 h-4 rounded-full border-2 border-t-transparent animate-spin"
                    style={{ borderColor: "rgba(0,212,255,0.4)", borderTopColor: "transparent" }}
                  />
                  验证中...
                </span>
              ) : (
                "登 录"
              )}
            </button>
          </div>
        </form>

        {/* Footer */}
        <div
          className="px-10 pb-8 text-center"
          style={{ borderTop: "1px solid rgba(0,212,255,0.08)" }}
        >
          <p className="text-xs mt-4" style={{ color: "rgba(200,216,232,0.4)" }}>
            © 2026 FrontCRM · 智能进销存管理平台 · v2.0.0
          </p>
        </div>
      </div>
    </div>
  );
}
