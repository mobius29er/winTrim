import Link from "next/link";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Privacy Policy - WinTrim",
  description: "WinTrim privacy policy. Your data stays on your machine.",
};

export default function PrivacyPage() {
  return (
    <main className="min-h-screen bg-[#050505] relative overflow-hidden">
      {/* Ambient Glow */}
      <div className="fixed top-0 left-1/2 -translate-x-1/2 w-[600px] h-[400px] bg-[#00F3FF] opacity-5 rounded-full blur-[150px] pointer-events-none" />

      {/* Navigation */}
      <nav className="fixed top-0 left-0 right-0 z-50 bg-[#050505]/80 backdrop-blur-md border-b border-white/5">
        <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
          <Link href="/" className="flex items-center gap-2">
            <div className="w-8 h-8 bg-gradient-to-br from-[#00F3FF] to-[#FF00FF] rounded-lg flex items-center justify-center">
              <span className="text-black font-bold text-sm" style={{ fontFamily: 'Rajdhani, sans-serif' }}>W</span>
            </div>
            <span className="text-xl font-bold tracking-wider" style={{ fontFamily: 'Orbitron, sans-serif' }}>
              WIN<span className="text-[#00F3FF]">TRIM</span>
            </span>
          </Link>
          <div className="flex items-center gap-6">
            <Link href="/" className="text-sm text-gray-400 hover:text-[#00F3FF] transition-colors tracking-wider uppercase">
              Home
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

      {/* Content */}
      <div className="pt-32 pb-20 px-6">
        <div className="max-w-3xl mx-auto">
          {/* Header */}
          <div className="mb-12">
            <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full border border-[#00F3FF]/30 bg-[#00F3FF]/5 mb-6">
              <div className="w-2 h-2 bg-[#00F3FF] rounded-full" />
              <span className="text-xs text-[#00F3FF] tracking-[0.2em] uppercase">Data_Protocol</span>
            </div>
            <h1 
              className="text-4xl md:text-5xl font-bold mb-4"
              style={{ fontFamily: 'Orbitron, sans-serif' }}
            >
              <span className="text-transparent bg-clip-text bg-gradient-to-r from-[#00F3FF] to-[#FF00FF]">
                PRIVACY
              </span>
              <br />
              <span className="text-white">POLICY</span>
            </h1>
            <p className="text-gray-500 text-sm">
              Last updated: February 2026
            </p>
          </div>

          {/* Policy Content */}
          <div className="space-y-8 text-gray-300">
            <section className="glass-card rounded-xl p-6 border border-[#00F3FF]/20">
              <h2 className="text-[#00F3FF] text-xl font-bold mb-4 tracking-wider uppercase" style={{ fontFamily: 'Rajdhani' }}>
                // Summary
              </h2>
              <p className="text-lg leading-relaxed">
                <strong className="text-white">WinTrim does not collect, store, or transmit any of your data.</strong> 
                {" "}The application runs entirely on your local machine with zero network connectivity required.
              </p>
            </section>

            <section className="glass-card rounded-xl p-6 border border-gray-800/50">
              <h2 className="text-white text-xl font-bold mb-4 tracking-wider uppercase" style={{ fontFamily: 'Rajdhani' }}>
                // Data Collection
              </h2>
              <p className="mb-4">WinTrim collects <span className="text-[#00F3FF]">zero data</span>. Specifically:</p>
              <ul className="space-y-2 text-gray-400">
                <li className="flex items-start gap-3">
                  <span className="text-[#00F3FF]">→</span>
                  <span>No personal information is collected</span>
                </li>
                <li className="flex items-start gap-3">
                  <span className="text-[#00F3FF]">→</span>
                  <span>No file names, paths, or contents are transmitted</span>
                </li>
                <li className="flex items-start gap-3">
                  <span className="text-[#00F3FF]">→</span>
                  <span>No usage analytics or telemetry</span>
                </li>
                <li className="flex items-start gap-3">
                  <span className="text-[#00F3FF]">→</span>
                  <span>No crash reports sent automatically</span>
                </li>
                <li className="flex items-start gap-3">
                  <span className="text-[#00F3FF]">→</span>
                  <span>No network requests of any kind</span>
                </li>
              </ul>
            </section>

            <section className="glass-card rounded-xl p-6 border border-gray-800/50">
              <h2 className="text-white text-xl font-bold mb-4 tracking-wider uppercase" style={{ fontFamily: 'Rajdhani' }}>
                // Local Processing
              </h2>
              <p className="text-gray-400 leading-relaxed">
                All disk analysis, file scanning, and cleanup suggestions are processed entirely on your local machine. 
                Your file system data never leaves your computer. The application does not require an internet connection 
                to function and makes no outbound network connections.
              </p>
            </section>

            <section className="glass-card rounded-xl p-6 border border-gray-800/50">
              <h2 className="text-white text-xl font-bold mb-4 tracking-wider uppercase" style={{ fontFamily: 'Rajdhani' }}>
                // Open Source Verification
              </h2>
              <p className="text-gray-400 leading-relaxed mb-4">
                WinTrim is open source software. You can verify these privacy claims by reviewing the source code:
              </p>
              <a
                href="https://github.com/mobius29er/winTrim"
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center gap-2 text-[#00F3FF] hover:underline"
              >
                <span>github.com/mobius29er/winTrim</span>
                <span>→</span>
              </a>
            </section>

            <section className="glass-card rounded-xl p-6 border border-gray-800/50">
              <h2 className="text-white text-xl font-bold mb-4 tracking-wider uppercase" style={{ fontFamily: 'Rajdhani' }}>
                // macOS-Specific Permissions
              </h2>
              <p className="text-gray-400 leading-relaxed mb-4">
                WinTrim for macOS may request the following system permissions to provide disk analysis features.
                All data processing remains entirely local on your machine:
              </p>
              <div className="space-y-4">
                <div>
                  <h3 className="text-white font-semibold mb-2">Full Disk Access (Optional)</h3>
                  <p className="text-gray-400 text-sm leading-relaxed mb-2">
                    Enables comprehensive disk scanning and Time Machine backup analysis. You can choose to grant or deny this permission—WinTrim will work with user-selected folders if denied.
                  </p>
                  <ul className="space-y-1 text-gray-500 text-sm ml-4">
                    <li className="flex items-start gap-2">
                      <span className="text-[#00F3FF]">•</span>
                      <span>Analyze complete file system for accurate disk usage reports</span>
                    </li>
                    <li className="flex items-start gap-2">
                      <span className="text-[#00F3FF]">•</span>
                      <span>Detect system files and hidden caches</span>
                    </li>
                    <li className="flex items-start gap-2">
                      <span className="text-[#00F3FF]">•</span>
                      <span>Analyze Time Machine backups (macOS only)</span>
                    </li>
                  </ul>
                </div>
                <div>
                  <h3 className="text-white font-semibold mb-2">Folder Access</h3>
                  <p className="text-gray-400 text-sm leading-relaxed">
                    When you select a folder to scan, WinTrim requests access only to that specific folder.
                    These permissions use macOS security-scoped bookmarks and can be revoked anytime through System Settings → Privacy & Security.
                  </p>
                </div>
                <div>
                  <h3 className="text-white font-semibold mb-2">Time Machine Analysis</h3>
                  <p className="text-gray-400 text-sm leading-relaxed">
                    Reads metadata about backup snapshots to identify large files and suggest exclusions.
                    This feature runs entirely locally, does not modify backups, and does not transmit any information.
                  </p>
                </div>
                <div>
                  <h3 className="text-white font-semibold mb-2">Desktop Folder Access</h3>
                  <p className="text-gray-400 text-sm leading-relaxed">
                    Creates a &quot;DeleteMe&quot; folder on your Desktop to safely stage files for cleanup.
                    Files moved here remain on your computer until you manually delete them.
                  </p>
                </div>
              </div>
              <div className="mt-4 p-3 bg-[#00F3FF]/5 border border-[#00F3FF]/20 rounded">
                <p className="text-[#00F3FF] text-sm">
                  <strong>Privacy Control:</strong> All permission requests use macOS standard dialogs.
                  You can grant, deny, or revoke access at any time through System Settings.
                </p>
              </div>
            </section>

            <section className="glass-card rounded-xl p-6 border border-gray-800/50">
              <h2 className="text-white text-xl font-bold mb-4 tracking-wider uppercase" style={{ fontFamily: 'Rajdhani' }}>
                // Third-Party Services
              </h2>
              <p className="text-gray-400 leading-relaxed">
                WinTrim does not integrate with any third-party services, APIs, or analytics platforms.
                There are no embedded trackers, advertising SDKs, or data collection libraries.
              </p>
            </section>

            <section className="glass-card rounded-xl p-6 border border-[#FF9900]/20">
              <h2 className="text-[#FF9900] text-xl font-bold mb-4 tracking-wider uppercase" style={{ fontFamily: 'Rajdhani' }}>
                // Contact
              </h2>
              <p className="text-gray-400 leading-relaxed mb-4">
                If you have questions about this privacy policy or the application:
              </p>
              <a 
                href="mailto:support@foxxception.com"
                className="text-[#00F3FF] hover:underline"
              >
                support@foxxception.com
              </a>
            </section>
          </div>

          {/* Back Link */}
          <div className="mt-12 pt-8 border-t border-white/5">
            <Link 
              href="/"
              className="inline-flex items-center gap-2 text-gray-500 hover:text-[#00F3FF] transition-colors text-sm tracking-wider uppercase"
            >
              <span>←</span>
              <span>Return to Home</span>
            </Link>
          </div>
        </div>
      </div>

      {/* Footer */}
      <footer className="border-t border-white/5 py-8 px-6">
        <div className="max-w-6xl mx-auto text-center">
          <p className="text-xs text-gray-600">
            © {new Date().getFullYear()} Foxxception. Open Source.
          </p>
        </div>
      </footer>
    </main>
  );
}
