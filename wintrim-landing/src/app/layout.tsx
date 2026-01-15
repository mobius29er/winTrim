import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "WinTrim - Precision Disk Analyzer for Windows",
  description: "A surgical tool for reclaiming disk space. Visualize, analyze, and optimize your Windows storage with zero telemetry.",
  keywords: ["disk analyzer", "windows", "storage", "cleanup", "disk space", "file scanner"],
  authors: [{ name: "Foxxception" }],
  openGraph: {
    title: "WinTrim - Precision Disk Analyzer",
    description: "Reclaim your disk space with surgical precision.",
    url: "https://wintrim.io",
    siteName: "WinTrim",
    type: "website",
  },
  twitter: {
    card: "summary_large_image",
    title: "WinTrim - Precision Disk Analyzer",
    description: "Reclaim your disk space with surgical precision.",
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className="scanlines antialiased">
        {children}
      </body>
    </html>
  );
}
