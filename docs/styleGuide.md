This is a fantastic pivot. The "Blade Runner" aesthetic (Tech-Noir) fits **WinTrim** perfectly because it appeals directly to your target audience: developers, gamers, and power users who love dark mode, terminals, and high-tech efficiency.

Unlike the generic "SaaS Blue" that looks like a corporate scam, this style says: **"This is a precision tool from the future."**

Here is your **WinTrim "Replicant" Design System**.

### 1. The Vibe: "Blade Runner 2049" (Not 1982)

You want the **2049** look: Clean, atmospheric, vast negative space, and precise neon accents. Avoid the "1982" look (gritty, dirty, chaotic) because dirty UIs make people think of malware.

* **Core Philosophy:** "High-Tech Zen."
* **The Metaphor:** WinTrim is a scalpel. It is sharp, precise, and illuminating.

### 2. The Color Palette (Neon & Void)

Forget standard black. You want "Void" colors that feel deep and atmospheric.

| Color Name | Hex Code | Usage |
| --- | --- | --- |
| **Void Black** | `#050505` | Main Background (The darkest dark). |
| **Wallace Amber** | `#FF9900` | **Primary Action.** (Download buttons, Risk Warnings). Use sparingly. |
| **Joi Pink** | `#FF00FF` | **Accents.** (Gradients, secondary glows). |
| **K Teal** | `#00F3FF` | **Data/Safe.** (Charts, "Safe" indicators, "Clean" status). |
| **Off-World Gray** | `#1F2937` | **Cards/Panels.** The background for your bento grid boxes. |

### 3. Typography

* **Headings:** **Orbitron** or **Rajdhani** (Google Fonts). They have that squared-off, futuristic look without being unreadable.
* **Body / Data:** **JetBrains Mono** or **Fira Code**.
* *Why:* Monospace fonts scream "Terminal." It makes your file analysis data look like raw, honest code.



### 4. UI Elements & Effects

#### A. The "Glass & Neon" Card

Don't use solid borders. Use **Inner Glows** and **Glassmorphism**.

* *CSS Concept:* A semi-transparent dark gray background with a 1px border that is only visible as a faint glow.
* *Hover Effect:* The border lights up Cyan or Amber when you touch it.

#### B. The "Scanline" Overlay

A very subtle CRT scanline effect over the entire page adds texture without reducing readability.

#### C. The "Hologram" Hero

The main image of your software shouldn't just be a screenshot. It should look like a **holographic projection**.

* *Effect:* Tilt the screenshot in 3D (CSS `perspective`), add a faint blue drop shadow, and a reflection below it.

---

### 5. The "Bento Grid" Code (Blade Runner Style)

Here is the HTML/Tailwind code for your features section. This uses the colors and fonts discussed above.

*(You will need Tailwind CSS installed, or just read the classes to understand the styling)*

```html
<section class="bg-[#050505] min-h-screen p-10 font-mono text-gray-300 relative overflow-hidden">
  
  <div class="absolute top-0 left-1/2 -translate-x-1/2 w-[800px] h-[500px] bg-[#00F3FF] opacity-5 rounded-full blur-[120px]"></div>

  <div class="max-w-6xl mx-auto relative z-10">
    
    <h2 class="text-4xl md:text-6xl font-bold text-transparent bg-clip-text bg-gradient-to-r from-[#00F3FF] to-[#FF00FF] mb-12 text-center" style="font-family: 'Rajdhani', sans-serif; letter-spacing: 0.1em;">
      SYSTEM DIAGNOSTICS
    </h2>

    <div class="grid grid-cols-1 md:grid-cols-4 grid-rows-3 gap-6 h-auto md:h-[600px]">

      <div class="md:col-span-2 md:row-span-3 border border-[#00F3FF]/20 bg-[#0A0A0A]/80 backdrop-blur-md rounded-lg p-6 hover:border-[#00F3FF]/60 transition-all duration-500 group shadow-[0_0_15px_rgba(0,243,255,0.05)]">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-[#00F3FF] text-xl tracking-widest uppercase">Visualizer</h3>
          <span class="text-xs border border-[#00F3FF] px-2 py-1 rounded text-[#00F3FF] group-hover:bg-[#00F3FF] group-hover:text-black transition-colors">ACTIVE</span>
        </div>
        <p class="text-sm text-gray-400 mb-6">Full-spectrum recursive drive scanning. Rendering treemap visualization of 1,000,000+ files in real-time.</p>
        <div class="w-full h-64 bg-gray-900 rounded border border-dashed border-gray-700 flex items-center justify-center relative overflow-hidden">
          <div class="absolute inset-0 bg-gradient-to-b from-transparent to-[#00F3FF]/10"></div>
          <span class="text-gray-600">[HOLOGRAPHIC_VIEWPORT_RENDER]</span>
        </div>
      </div>

      <div class="md:col-span-2 md:row-span-1 border border-[#FF9900]/20 bg-[#0A0A0A]/80 backdrop-blur-md rounded-lg p-6 hover:border-[#FF9900]/60 transition-all duration-500 group">
        <div class="flex items-center gap-3 mb-2">
          <div class="w-2 h-2 bg-[#FF9900] rounded-full animate-pulse"></div>
          <h3 class="text-[#FF9900] text-lg tracking-widest uppercase">Dev_Environment</h3>
        </div>
        <p class="text-sm text-gray-400">Targeting hidden artifacts: <span class="text-white">Docker VHDX</span>, <span class="text-white">Android AVDs</span>, <span class="text-white">Gradle</span>.</p>
      </div>

      <div class="md:col-span-1 md:row-span-2 border border-[#FF00FF]/20 bg-[#0A0A0A]/80 backdrop-blur-md rounded-lg p-6 hover:border-[#FF00FF]/60 transition-all duration-500 flex flex-col justify-end">
        <div class="mb-4 text-4xl text-[#FF00FF]">🛡️</div>
        <h3 class="text-[#FF00FF] text-lg tracking-widest uppercase mb-2">Offline</h3>
        <p class="text-xs text-gray-400">Zero telemetry. Air-gapped logic. Your data never leaves the local machine.</p>
      </div>

      <div class="md:col-span-1 md:row-span-2 border border-gray-700 bg-[#0A0A0A]/80 backdrop-blur-md rounded-lg p-6 hover:border-gray-500 transition-all duration-500">
         <h3 class="text-white text-lg tracking-widest uppercase mb-2">Speed</h3>
         <div class="text-5xl font-bold text-white mb-2" style="font-family: 'Rajdhani'">0.4s</div>
         <p class="text-xs text-gray-500">Scan initialization time for 500GB SSD.</p>
      </div>

    </div>
    
    <div class="mt-12 text-center text-xs text-gray-600 uppercase tracking-[0.2em]">
      // Verified_By_Microsoft_Store // Build_Ver_1.0.2 // Status_Clean
    </div>
  </div>
</section>

```

### 6. Critical Trust Check

Because this style looks "Cyberpunk/Hacker," you risk scaring normal people.

* **The Fix:** You must use **"Clean" language** inside the "Dark" design.
* Don't use words like "Hack," "Inject," or "Force."
* DO use words like "Analyze," "Visualize," "Optimize," "Safety Protocol."
