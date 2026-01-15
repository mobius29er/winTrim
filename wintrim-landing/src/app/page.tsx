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
          </p>

          {/* CTA Buttons */}
          <div className="flex flex-col sm:flex-row items-center justify-center gap-4 mb-16">
            <a 
              href="https://github.com/mobius29er/winTrim"
              target="_blank"
              rel="noopener noreferrer"
              className="btn-cyber px-8 py-4 bg-gradient-to-r from-[#FF9900] to-[#FF6600] text-black font-bold rounded-lg tracking-wider uppercase hover:shadow-[0_0_30px_rgba(255,153,0,0.5)] transition-all duration-300"
              style={{ fontFamily: 'Rajdhani, sans-serif' }}
            >
              Download for Windows
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
              <div className="text-5xl font-bold text-white mb-2" style={{ fontFamily: 'Rajdhani' }}>
                &lt;1s
              </div>
              <p className="text-xs text-gray-500">
                Scan initialization time for standard SSD drives.
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
                AI-assisted cleanup suggestions with risk assessment. Safe, Low, Medium, High risk indicators.
              </p>
            </div>
          </div>

          {/* Status Bar */}
          <div className="mt-16 text-center text-xs text-gray-600 uppercase tracking-[0.2em]">
            // Verified_By_Microsoft_Store // Open_Source // Status_Clean
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
            Free. Open source. No account required.
          </p>
          <a 
            href="https://github.com/mobius29er/winTrim"
            target="_blank"
            rel="noopener noreferrer"
            className="btn-cyber inline-block px-12 py-5 bg-gradient-to-r from-[#FF9900] to-[#FF6600] text-black font-bold text-lg rounded-lg tracking-wider uppercase hover:shadow-[0_0_40px_rgba(255,153,0,0.6)] transition-all duration-300"
            style={{ fontFamily: 'Rajdhani, sans-serif' }}
          >
            Download WinTrim
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
