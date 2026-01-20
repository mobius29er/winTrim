import Link from "next/link";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Terms of Service - WinTrim",
  description: "WinTrim terms of service and end user license agreement.",
};

export default function TermsPage() {
  return (
    <main className="min-h-screen bg-[#050505] relative overflow-hidden">
      {/* Ambient Glow */}
      <div className="fixed top-0 left-1/2 -translate-x-1/2 w-[600px] h-[400px] bg-[#FF9900] opacity-5 rounded-full blur-[150px] pointer-events-none" />

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
            <Link href="/privacy" className="text-sm text-gray-400 hover:text-[#00F3FF] transition-colors tracking-wider uppercase">
              Privacy
            </Link>
          </div>
        </div>
      </nav>

      {/* Content */}
      <div className="pt-32 pb-20 px-6">
        <div className="max-w-3xl mx-auto">
          {/* Header */}
          <div className="mb-12">
            <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full border border-[#FF9900]/30 bg-[#FF9900]/5 mb-6">
              <div className="w-2 h-2 bg-[#FF9900] rounded-full" />
              <span className="text-xs text-[#FF9900] tracking-[0.2em] uppercase">Legal_Protocol</span>
            </div>
            <h1 
              className="text-4xl md:text-5xl font-bold mb-4"
              style={{ fontFamily: 'Orbitron, sans-serif' }}
            >
              <span className="text-transparent bg-clip-text bg-gradient-to-r from-[#FF9900] to-[#FF6600]">
                TERMS OF
              </span>
              <br />
              <span className="text-white">SERVICE</span>
            </h1>
            <p className="text-gray-500 text-sm">
              Last updated: January 2026
            </p>
          </div>

          {/* Terms Content */}
          <div className="space-y-8 text-gray-300">
            
            {/* Warning Banner */}
            <section className="rounded-xl p-6 border-2 border-[#FF9900]/50 bg-[#FF9900]/10">
              <div className="flex items-start gap-4">
                <span className="text-3xl">⚠️</span>
                <div>
                  <h2 className="text-[#FF9900] text-xl font-bold mb-2 tracking-wider uppercase" style={{ fontFamily: 'Rajdhani' }}>
                    Important Notice
                  </h2>
                  <p className="text-gray-300 leading-relaxed">
                    WinTrim is a disk cleanup utility that <strong className="text-white">permanently deletes files</strong>. 
                    Deleted files may not be recoverable. Always review items before cleaning and maintain backups of important data.
                  </p>
                </div>
              </div>
            </section>

            <section className="glass-card rounded-xl p-6 border border-gray-800/50">
              <h2 className="text-white text-xl font-bold mb-4 tracking-wider uppercase" style={{ fontFamily: 'Rajdhani' }}>
                // 1. Disclaimer of Warranty
              </h2>
              <p className="text-gray-400 leading-relaxed">
                WinTrim is provided <strong className="text-white">&quot;AS IS&quot;</strong> without warranty of any kind, 
                either expressed or implied, including but not limited to the implied warranties of merchantability 
                and fitness for a particular purpose. The entire risk as to the quality and performance of the 
                software is with you.
              </p>
            </section>

            <section className="glass-card rounded-xl p-6 border border-gray-800/50">
              <h2 className="text-white text-xl font-bold mb-4 tracking-wider uppercase" style={{ fontFamily: 'Rajdhani' }}>
                // 2. Limitation of Liability
              </h2>
              <p className="text-gray-400 leading-relaxed">
                In no event shall Foxxception LLC or its contributors be liable for any direct, indirect, incidental, 
                special, exemplary, or consequential damages (including, but not limited to, loss of data, business 
                interruption, or any other commercial damages or losses) arising out of the use or inability to use 
                this software.
              </p>
            </section>

            <section className="glass-card rounded-xl p-6 border border-[#FF5555]/30 bg-[#FF5555]/5">
              <h2 className="text-[#FF5555] text-xl font-bold mb-4 tracking-wider uppercase" style={{ fontFamily: 'Rajdhani' }}>
                // 3. Data Deletion Warning
              </h2>
              <p className="text-gray-400 leading-relaxed mb-4">
                This software is designed to identify and delete files from your computer. While WinTrim attempts 
                to identify safe-to-delete files, it cannot guarantee that deleted files are not important to you 
                or your system.
              </p>
              <div className="bg-[#FF5555]/10 rounded-lg p-4 border border-[#FF5555]/20">
                <p className="text-[#FF5555] font-bold text-center">
                  ⚠️ DELETED FILES MAY NOT BE RECOVERABLE ⚠️
                </p>
              </div>
            </section>

            <section className="glass-card rounded-xl p-6 border border-gray-800/50">
              <h2 className="text-white text-xl font-bold mb-4 tracking-wider uppercase" style={{ fontFamily: 'Rajdhani' }}>
                // 4. User Responsibility
              </h2>
              <p className="text-gray-400 leading-relaxed mb-4">
                You are solely responsible for:
              </p>
              <ul className="space-y-2 text-gray-400">
                <li className="flex items-start gap-3">
                  <span className="text-[#00F3FF]">→</span>
                  <span>Backing up important data before using cleanup features</span>
                </li>
                <li className="flex items-start gap-3">
                  <span className="text-[#00F3FF]">→</span>
                  <span>Reviewing files before deletion</span>
                </li>
                <li className="flex items-start gap-3">
                  <span className="text-[#00F3FF]">→</span>
                  <span>Understanding the risk levels indicated for each cleanup suggestion</span>
                </li>
                <li className="flex items-start gap-3">
                  <span className="text-[#00F3FF]">→</span>
                  <span>Any consequences of deleting files</span>
                </li>
              </ul>
            </section>

            <section className="glass-card rounded-xl p-6 border border-[#FF9900]/30 bg-[#FF9900]/5">
              <h2 className="text-[#FF9900] text-xl font-bold mb-4 tracking-wider uppercase" style={{ fontFamily: 'Rajdhani' }}>
                // 5. Use at Your Own Risk
              </h2>
              <p className="text-gray-300 leading-relaxed">
                By using WinTrim, you acknowledge that you use this software <strong className="text-white">at your own risk</strong>. 
                You agree to hold harmless Foxxception LLC and its affiliates from any claims arising from your use of this software.
              </p>
            </section>

            <section className="glass-card rounded-xl p-6 border border-gray-800/50">
              <h2 className="text-white text-xl font-bold mb-4 tracking-wider uppercase" style={{ fontFamily: 'Rajdhani' }}>
                // 6. Acceptance
              </h2>
              <p className="text-gray-400 leading-relaxed">
                By downloading, installing, or using WinTrim, you acknowledge that you have read, understood, and 
                agree to be bound by the terms of this agreement.
              </p>
            </section>

            <section className="glass-card rounded-xl p-6 border border-gray-800/50">
              <h2 className="text-white text-xl font-bold mb-4 tracking-wider uppercase" style={{ fontFamily: 'Rajdhani' }}>
                // 7. Contact
              </h2>
              <p className="text-gray-400 leading-relaxed mb-4">
                If you have questions about these terms:
              </p>
              <a 
                href="mailto:legal@foxxception.com"
                className="text-[#00F3FF] hover:underline"
              >
                legal@foxxception.com
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
            © {new Date().getFullYear()} Foxxception LLC. All rights reserved.
          </p>
        </div>
      </footer>
    </main>
  );
}
