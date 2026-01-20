import Link from "next/link";

export default function Home() {
  return (
    <main className="min-h-screen bg-[#050505] relative overflow-hidden">
      {/* Ambient Glow Effects */}
      <div className="fixed top-0 left-1/2 -translate-x-1/2 w-[800px] h-[500px] bg-[#00F3FF] opacity-5 rounded-full blur-[150px] pointer-events-none" />
      <div className="fixed bottom-0 right-0 w-[600px] h-[400px] bg-[#FF00FF] opacity-5 rounded-full blur-[150px] pointer-events-none" />

      {/* Navigation */}
      <nav className="fixed top-0 left-0 right-0 z-50 bg-[#050505]/80 backdrop-blur-md border-b border-white/5">
        <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 bg-gradient-to-br from-[#00F3FF] to-[#FF00FF] rounded-lg flex items-center justify-center">
              <span className="text-black font-bold text-sm" style={{ fontFamily: 'Rajdhani, sans-serif' }}>W</span>
            </div>
            <span className="text-xl font-bold tracking-wider" style={{ fontFamily: 'Orbitron, sans-serif' }}>
              WIN<span className="text-[#00F3FF]">TRIM</span>
            </span>
          </div>
          <div className="flex items-center gap-6">
            <Link href="#features" className="text-sm text-gray-400 hover:text-[#00F3FF] transition-colors tracking-wider uppercase">
              Features
            </Link>
            <Link href="/privacy" className="text-sm text-gray-400 hover:text-[#00F3FF] transition-colors tracking-wider uppercase">
              Privacy
            </Link>
            <a 
              href="https://github.com/mobius29er/winTrim" 
              target="_blank" 
              rel="noopener noreferrer"
              className="text-sm text-gray-400 hover:text-[#00F3FF] transition-colors tracking-wider uppercase"
            >
              GitHub
            </a>
          </div>
        </div>
      </nav>

      {/* Hero Section */}
      <section className="relative min-h-screen flex items-center justify-center pt-20 px-6">
        <div className="max-w-6xl mx-auto text-center">
          {/* Status Badge */}
          <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full border border-[#00F3FF]/30 bg-[#00F3FF]/5 mb-8">
            <div className="w-2 h-2 bg-[#00F3FF] rounded-full pulse-glow" />
            <span className="text-xs text-[#00F3FF] tracking-[0.2em] uppercase">System Online</span>
          </div>

          {/* Main Headline */}
          <h1 
            className="text-5xl md:text-7xl lg:text-8xl font-bold mb-6 tracking-tight"
            style={{ fontFamily: 'Orbitron, sans-serif' }}
          >
            <span className="text-transparent bg-clip-text bg-gradient-to-r from-[#00F3FF] via-white to-[#FF00FF]">
              RECLAIM
            </span>
            <br />
            <span className="text-white">YOUR SPACE</span>
          </h1>

          {/* Subheadline */}
          <p className="text-lg md:text-xl text-gray-400 max-w-2xl mx-auto mb-12 leading-relaxed">
            A precision disk analyzer that visualizes, identifies, and eliminates 
            storage waste with <span className="text-[#00F3FF]">surgical accuracy</span>. 
            Available on <span className="text-white">Windows</span>, <span className="text-white">macOS</span>, and <span className="text-white">Linux</span>.
          </p>

          {/* CTA Buttons */}
          <div className="flex flex-col sm:flex-row items-center justify-center gap-4 mb-8">
            <a 
              href="https://github.com/mobius29er/winTrim/releases"
              target="_blank"
              rel="noopener noreferrer"
              className="btn-cyber px-8 py-4 bg-gradient-to-r from-[#FF9900] to-[#FF6600] text-black font-bold rounded-lg tracking-wider uppercase hover:shadow-[0_0_30px_rgba(255,153,0,0.5)] transition-all duration-300"
              style={{ fontFamily: 'Rajdhani, sans-serif' }}
            >
              Download Now — $4.99
            </a>
            <a 
              href="https://github.com/mobius29er/winTrim"
              target="_blank"
              rel="noopener noreferrer"
              className="px-8 py-4 border border-[#00F3FF]/30 text-[#00F3FF] font-bold rounded-lg tracking-wider uppercase hover:bg-[#00F3FF]/10 hover:border-[#00F3FF]/60 transition-all duration-300"
              style={{ fontFamily: 'Rajdhani, sans-serif' }}
            >
              View Source
            </a>
          </div>

          {/* Platform Badges */}
          <div className="flex items-center justify-center gap-6 mb-16">
            <div className="flex items-center gap-2 text-gray-400">
              <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 24 24"><path d="M0 3.449L9.75 2.1v9.451H0m10.949-9.602L24 0v11.4H10.949M0 12.6h9.75v9.451L0 20.699M10.949 12.6H24V24l-12.9-1.801"/></svg>
              <span className="text-sm">Windows</span>
            </div>
            <div className="flex items-center gap-2 text-gray-400">
              <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 24 24"><path d="M12 2C6.477 2 2 6.477 2 12s4.477 10 10 10 10-4.477 10-10S17.523 2 12 2zm0 18c-4.418 0-8-3.582-8-8s3.582-8 8-8 8 3.582 8 8-3.582 8-8 8zm-1-13h2v6h-2zm0 8h2v2h-2z"/></svg>
              <span className="text-sm">macOS</span>
            </div>
            <div className="flex items-center gap-2 text-gray-400">
              <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 24 24"><path d="M12.503 0c-.155 0-.31.004-.463.012-2.073.074-3.962.738-5.407 1.87C5.187 3.013 4.092 4.573 3.553 6.4c-.54 1.828-.479 3.89.17 5.678.536 1.476 1.397 2.727 2.448 3.684a8.49 8.49 0 0 0-.154 1.605c0 1.257.264 2.438.734 3.514.47 1.076 1.138 2.035 1.954 2.78.815.746 1.764 1.278 2.787 1.574 1.022.297 2.097.367 3.142.206 1.046-.16 2.044-.552 2.914-1.131.871-.58 1.604-1.333 2.142-2.2.538-.866.876-1.834.992-2.83.117-.996.01-2.007-.318-2.96a7.56 7.56 0 0 0-1.434-2.518c.573-1.312.846-2.736.8-4.156-.045-1.42-.41-2.821-1.073-4.06-.663-1.239-1.6-2.283-2.725-3.065C14.785.52 13.478.08 12.126.007 12.252.003 12.377 0 12.503 0z"/></svg>
              <span className="text-sm">Linux</span>
            </div>
          </div>

          {/* Holographic Screenshot */}
          <div className="relative max-w-4xl mx-auto">
            <div className="hologram rounded-lg overflow-hidden border border-[#00F3FF]/20">
              <div className="bg-[#0A0A0A] p-4 border-b border-[#00F3FF]/10">
                <div className="flex items-center gap-2">
                  <div className="w-3 h-3 rounded-full bg-[#FF5F57]" />
                  <div className="w-3 h-3 rounded-full bg-[#FEBC2E]" />
                  <div className="w-3 h-3 rounded-full bg-[#28C840]" />
                  <span className="ml-4 text-xs text-gray-500 tracking-wider">WinTrim v1.0</span>
                </div>
              </div>
              <div className="bg-gradient-to-br from-[#0A0A0A] to-[#111] p-8 min-h-[300px] flex items-center justify-center">
                <div className="text-center">
                  <div className="grid grid-cols-3 gap-4 mb-6">
                    <div className="p-4 rounded-lg bg-[#00F3FF]/10 border border-[#00F3FF]/20">
                      <div className="text-2xl font-bold text-[#00F3FF]" style={{ fontFamily: 'Rajdhani' }}>127 GB</div>
                      <div className="text-xs text-gray-500 uppercase tracking-wider">Analyzed</div>
                    </div>
                    <div className="p-4 rounded-lg bg-[#FF9900]/10 border border-[#FF9900]/20">
                      <div className="text-2xl font-bold text-[#FF9900]" style={{ fontFamily: 'Rajdhani' }}>23.4 GB</div>
                      <div className="text-xs text-gray-500 uppercase tracking-wider">Recoverable</div>
                    </div>
                    <div className="p-4 rounded-lg bg-[#FF00FF]/10 border border-[#FF00FF]/20">
                      <div className="text-2xl font-bold text-[#FF00FF]" style={{ fontFamily: 'Rajdhani' }}>1.2M</div>
                      <div className="text-xs text-gray-500 uppercase tracking-wider">Files</div>
                    </div>
                  </div>
                  <div className="text-xs text-gray-600 tracking-[0.15em] uppercase">
                    // Interactive_Visualization_Viewport
                  </div>
                </div>
              </div>
            </div>
            {/* Reflection */}
            <div className="absolute -bottom-20 left-0 right-0 h-20 bg-gradient-to-b from-[#00F3FF]/5 to-transparent blur-sm opacity-50 transform scale-y-[-1]" />
          </div>
        </div>
      </section>

      {/* Features Bento Grid */}
      <section id="features" className="relative py-32 px-6">
        <div className="max-w-6xl mx-auto">
          <h2 
            className="text-4xl md:text-6xl font-bold text-center mb-4 tracking-wider"
            style={{ fontFamily: 'Rajdhani, sans-serif', letterSpacing: '0.1em' }}
          >
            <span className="text-transparent bg-clip-text bg-gradient-to-r from-[#00F3FF] to-[#FF00FF]">
              SYSTEM DIAGNOSTICS
            </span>
          </h2>
          <p className="text-center text-gray-500 text-sm tracking-[0.2em] uppercase mb-16">
            // Core_Module_Analysis
          </p>

          <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
            {/* Large Card - Visualizer */}
            <div className="md:col-span-2 md:row-span-2 glass-card rounded-xl p-8 border border-[#00F3FF]/20 hover:border-[#00F3FF]/50 group">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-[#00F3FF] text-xl tracking-widest uppercase" style={{ fontFamily: 'Rajdhani' }}>
                  Visualizer
                </h3>
                <span className="text-xs border border-[#00F3FF]/50 px-3 py-1 rounded-full text-[#00F3FF] group-hover:bg-[#00F3FF] group-hover:text-black transition-colors">
                  ACTIVE
                </span>
              </div>
              <p className="text-sm text-gray-400 mb-6 leading-relaxed">
                Full-spectrum recursive drive scanning. Rendering treemap visualization 
                of millions of files in real-time with interactive drill-down.
              </p>
              <div className="w-full h-48 bg-[#0A0A0A] rounded-lg border border-dashed border-gray-800 flex items-center justify-center relative overflow-hidden">
                <div className="absolute inset-0 bg-gradient-to-b from-transparent to-[#00F3FF]/5" />
                <div className="grid grid-cols-4 grid-rows-3 gap-1 w-full h-full p-2">
                  <div className="col-span-2 row-span-2 bg-[#00F3FF]/20 rounded" />
                  <div className="bg-[#FF9900]/20 rounded" />
                  <div className="bg-[#FF00FF]/20 rounded" />
                  <div className="bg-[#00F3FF]/10 rounded" />
                  <div className="col-span-2 bg-[#FF9900]/10 rounded" />
                </div>
              </div>
            </div>

            {/* Dev Environment Card */}
            <div className="md:col-span-2 glass-card rounded-xl p-6 border border-[#FF9900]/20 hover:border-[#FF9900]/50 group">
              <div className="flex items-center gap-3 mb-3">
                <div className="w-2 h-2 bg-[#FF9900] rounded-full pulse-glow" />
                <h3 className="text-[#FF9900] text-lg tracking-widest uppercase" style={{ fontFamily: 'Rajdhani' }}>
                  Dev_Environment
                </h3>
              </div>
              <p className="text-sm text-gray-400">
                Targeting hidden artifacts: <span className="text-white">Docker VHDX</span>, 
                <span className="text-white"> Android AVDs</span>, <span className="text-white">Gradle</span>, 
                <span className="text-white"> node_modules</span>, <span className="text-white">NuGet cache</span>.
              </p>
            </div>

            {/* Offline Card */}
            <div className="glass-card rounded-xl p-6 border border-[#FF00FF]/20 hover:border-[#FF00FF]/50 flex flex-col justify-between">
              <div>
                <div className="mb-4 text-4xl">&#128737;</div>
                <h3 className="text-[#FF00FF] text-lg tracking-widest uppercase mb-2" style={{ fontFamily: 'Rajdhani' }}>
                  Offline
                </h3>
              </div>
              <p className="text-xs text-gray-400">
                Zero telemetry. Air-gapped logic. Your data never leaves the local machine.
              </p>
            </div>

            {/* Speed Card */}
            <div className="glass-card rounded-xl p-6 border border-gray-700/50 hover:border-gray-500/50">
              <h3 className="text-white text-lg tracking-widest uppercase mb-2" style={{ fontFamily: 'Rajdhani' }}>
                Speed
              </h3>
              <div className="text-3xl font-bold text-white mb-2" style={{ fontFamily: 'Rajdhani' }}>
                1TB in 30s
              </div>
              <p className="text-xs text-gray-500">
                Blazing fast on Windows. ~2 min on Mac. Full disk analysis.
              </p>
            </div>

            {/* Game Detection Card */}
            <div className="glass-card rounded-xl p-6 border border-[#00F3FF]/20 hover:border-[#00F3FF]/50">
              <div className="flex items-center gap-2 mb-3">
                <span className="text-2xl">&#127918;</span>
                <h3 className="text-[#00F3FF] text-lg tracking-widest uppercase" style={{ fontFamily: 'Rajdhani' }}>
                  Games
                </h3>
              </div>
              <p className="text-xs text-gray-400">
                Detect Steam, Epic, GOG installations. Identify orphaned game files and massive save folders.
              </p>
            </div>

            {/* Smart Cleanup Card */}
            <div className="glass-card rounded-xl p-6 border border-[#FF9900]/20 hover:border-[#FF9900]/50">
              <div className="flex items-center gap-2 mb-3">
                <span className="text-2xl">&#129529;</span>
                <h3 className="text-[#FF9900] text-lg tracking-widest uppercase" style={{ fontFamily: 'Rajdhani' }}>
                  Smart Clean
                </h3>
              </div>
              <p className="text-xs text-gray-400">
                Intelligent cleanup with risk assessment. Preview files with size, date, and risk level before deleting.
              </p>
            </div>

            {/* Session Persistence Card */}
            <div className="glass-card rounded-xl p-6 border border-[#00F3FF]/20 hover:border-[#00F3FF]/50">
              <div className="flex items-center gap-2 mb-3">
                <span className="text-2xl">💾</span>
                <h3 className="text-[#00F3FF] text-lg tracking-widest uppercase" style={{ fontFamily: 'Rajdhani' }}>
                  Auto-Save
                </h3>
              </div>
              <p className="text-xs text-gray-400">
                Your scan results persist between sessions. Pick up right where you left off.
              </p>
            </div>
          </div>

          {/* Status Bar */}
          <div className="mt-16 text-center text-xs text-gray-600 uppercase tracking-[0.2em]">
            // Cross_Platform // One_Time_Purchase // No_Subscription // Open_Source
          </div>
        </div>
      </section>

      {/* Coming Soon / Roadmap Teaser */}
      <section className="relative py-20 px-6 border-t border-white/5">
        <div className="max-w-4xl mx-auto text-center">
          <h3 
            className="text-2xl md:text-3xl font-bold mb-8 tracking-wider"
            style={{ fontFamily: 'Rajdhani, sans-serif' }}
          >
            <span className="text-transparent bg-clip-text bg-gradient-to-r from-[#00F3FF] to-[#FF00FF]">
              COMING SOON
            </span>
          </h3>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="glass-card rounded-xl p-6 border border-gray-700/30">
              <div className="text-2xl mb-3">🍎</div>
              <h4 className="text-white text-sm uppercase tracking-wider mb-2" style={{ fontFamily: 'Rajdhani' }}>Mac App Store</h4>
              <p className="text-xs text-gray-500">Official App Store listing for seamless installation</p>
            </div>
            <div className="glass-card rounded-xl p-6 border border-gray-700/30">
              <div className="text-2xl mb-3">📦</div>
              <h4 className="text-white text-sm uppercase tracking-wider mb-2" style={{ fontFamily: 'Rajdhani' }}>Duplicate Detection</h4>
              <p className="text-xs text-gray-500">Find and remove duplicate files across your drives</p>
            </div>
            <div className="glass-card rounded-xl p-6 border border-gray-700/30">
              <div className="text-2xl mb-3">🌐</div>
              <h4 className="text-white text-sm uppercase tracking-wider mb-2" style={{ fontFamily: 'Rajdhani' }}>Browser Cleanup</h4>
              <p className="text-xs text-gray-500">Chrome, Firefox, Safari, Edge cache detection</p>
            </div>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="relative py-32 px-6">
        <div className="max-w-3xl mx-auto text-center">
          <h2 
            className="text-4xl md:text-5xl font-bold mb-6"
            style={{ fontFamily: 'Orbitron, sans-serif' }}
          >
            <span className="text-white">INITIALIZE</span>
            <br />
            <span className="text-transparent bg-clip-text bg-gradient-to-r from-[#00F3FF] to-[#FF00FF]">
              SEQUENCE
            </span>
          </h2>
          <p className="text-gray-400 mb-8">
            One-time purchase. No subscription. No account required.
          </p>
          <a 
            href="https://github.com/mobius29er/winTrim/releases"
            target="_blank"
            rel="noopener noreferrer"
            className="btn-cyber inline-block px-12 py-5 bg-gradient-to-r from-[#FF9900] to-[#FF6600] text-black font-bold text-lg rounded-lg tracking-wider uppercase hover:shadow-[0_0_40px_rgba(255,153,0,0.6)] transition-all duration-300"
            style={{ fontFamily: 'Rajdhani, sans-serif' }}
          >
            Get WinTrim — $4.99
          </a>
        </div>
      </section>

      {/* Footer */}
      <footer className="border-t border-white/5 py-12 px-6">
        <div className="max-w-6xl mx-auto">
          <div className="flex flex-col md:flex-row items-center justify-between gap-6">
            {/* Logo */}
            <div className="flex items-center gap-2">
              <div className="w-6 h-6 bg-gradient-to-br from-[#00F3FF] to-[#FF00FF] rounded flex items-center justify-center">
                <span className="text-black font-bold text-xs" style={{ fontFamily: 'Rajdhani' }}>W</span>
              </div>
              <span className="text-sm tracking-wider" style={{ fontFamily: 'Orbitron' }}>
                WIN<span className="text-[#00F3FF]">TRIM</span>
              </span>
            </div>

            {/* Links */}
            <div className="flex items-center gap-8 text-xs text-gray-500 uppercase tracking-wider">
              <Link href="/privacy" className="hover:text-[#00F3FF] transition-colors">
                Privacy
              </Link>
              <a 
                href="https://github.com/mobius29er/winTrim" 
                target="_blank" 
                rel="noopener noreferrer"
                className="hover:text-[#00F3FF] transition-colors"
              >
                GitHub
              </a>
              <a 
                href="mailto:support@foxxception.com"
                className="hover:text-[#00F3FF] transition-colors"
              >
                Contact
              </a>
            </div>

            {/* Copyright */}
            <div className="text-xs text-gray-600">
              2026 Foxxception. Open Source.
            </div>
          </div>

          {/* Attribution */}
          <div className="mt-8 pt-8 border-t border-white/5 text-center">
            <p className="text-xs text-gray-600">
              Built with precision by{" "}
              <a 
                href="https://github.com/mobius29er" 
                target="_blank" 
                rel="noopener noreferrer"
                className="text-[#00F3FF]/60 hover:text-[#00F3FF] transition-colors"
              >
                @mobius29er
              </a>
              {" "}// Source available on{" "}
              <a 
                href="https://github.com/mobius29er/winTrim" 
                target="_blank" 
                rel="noopener noreferrer"
                className="text-[#00F3FF]/60 hover:text-[#00F3FF] transition-colors"
              >
                GitHub
              </a>
            </p>
          </div>
        </div>
      </footer>
    </main>
  );
}
