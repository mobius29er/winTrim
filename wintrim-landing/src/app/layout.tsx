import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  metadataBase: new URL('https://wintrim.io'),
  title: "WinTrim - Cross-Platform Disk Analyzer for Windows, macOS & Linux",
  description: "A fast, visual disk analyzer that finds wasted space in seconds. Detects games, dev tools, and caches automatically. Free, open source, no telemetry.",
  keywords: [
    "disk analyzer", 
    "disk space analyzer",
    "storage cleanup", 
    "file scanner",
    "treemap visualization",
    "windows disk analyzer",
    "mac disk analyzer",
    "linux disk analyzer",
    "free disk space",
    "game storage",
    "developer cache cleanup",
    "xcode derived data",
    "node_modules cleanup",
    "avalonia",
    "cross-platform",
    "open source"
  ],
  authors: [{ name: "Foxxception", url: "https://github.com/mobius29er" }],
  creator: "Foxxception",
  publisher: "WinTrim",
  robots: {
    index: true,
    follow: true,
    googleBot: {
      index: true,
      follow: true,
      'max-video-preview': -1,
      'max-image-preview': 'large',
      'max-snippet': -1,
    },
  },
  openGraph: {
    title: "WinTrim - Cross-Platform Disk Analyzer",
    description: "Find wasted disk space in seconds. Detects games, dev tools, and caches automatically. Free & open source.",
    url: "https://wintrim.io",
    siteName: "WinTrim",
    type: "website",
    locale: "en_US",
    images: [
      {
        url: "/screenshot-hero.png",
        width: 1200,
        height: 630,
        alt: "WinTrim disk analyzer scanning 1.81 TB drive",
      },
    ],
  },
  twitter: {
    card: "summary_large_image",
    title: "WinTrim - Cross-Platform Disk Analyzer",
    description: "Find wasted disk space in seconds. Free & open source.",
    images: ["/screenshot-hero.png"],
  },
  alternates: {
    canonical: "https://wintrim.io",
  },
  category: "Technology",
  classification: "Disk Utility Software",
};

// JSON-LD Structured Data
const jsonLd = {
  "@context": "https://schema.org",
  "@type": "SoftwareApplication",
  "name": "WinTrim",
  "applicationCategory": "UtilitiesApplication",
  "operatingSystem": ["Windows", "macOS", "Linux"],
  "offers": {
    "@type": "Offer",
    "price": "0",
    "priceCurrency": "USD",
    "availability": "https://schema.org/InStock"
  },
  "description": "A fast, visual disk analyzer that finds wasted space in seconds. Detects games, dev tools, and caches automatically.",
  "url": "https://wintrim.io",
  "downloadUrl": "https://github.com/mobius29er/winTrim",
  "softwareVersion": "1.0",
  "author": {
    "@type": "Person",
    "name": "Foxxception",
    "url": "https://github.com/mobius29er"
  },
  "license": "https://opensource.org/licenses/MIT",
  "featureList": [
    "Cross-platform (Windows, macOS, Linux)",
    "Fast disk scanning",
    "Treemap visualization",
    "Game detection",
    "Developer tools detection",
    "No telemetry"
  ]
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <head>
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
        />
      </head>
      <body className="scanlines antialiased">
        {children}
      </body>
    </html>
  );
}
