<template>
  <div class="app-shell">
    <aside class="sidebar">
      <div class="brand">
        <span class="brand-mark">TC</span>
        <div>
          <p class="eyebrow">Authoring</p>
          <h1>TemplarCMS Admin</h1>
        </div>
      </div>

      <nav class="nav">
        <a class="nav-link nav-link--active" href="/">Overview</a>
        <a class="nav-link" href="/">Templates</a>
        <a class="nav-link" href="/">Content Tree</a>
        <a class="nav-link" href="/">API Contracts</a>
      </nav>

      <section class="sidebar-card">
        <p class="eyebrow">Current Focus</p>
        <p>Intentional authoring contracts, cleaner content APIs, and a frontend that feels built for editors rather than demos.</p>
      </section>
    </aside>

    <main class="main-panel">
      <section class="hero">
        <p class="eyebrow">Foundation Slice</p>
        <h2>Shape the admin surface around the CMS we are actually building.</h2>
        <p class="hero-copy">
          This shell intentionally starts small: templates, content navigation, dependency awareness,
          and safe authoring workflows. The goal is clarity first, not dashboard noise.
        </p>
      </section>

      <section class="status-grid">
        <article class="status-card">
          <p class="eyebrow">Templates</p>
          <h3>Modeled for authors</h3>
          <p>Effective templates, dependency preflight, and future-friendly inheritance contracts are already part of the API shape.</p>
        </article>

        <article class="status-card">
          <p class="eyebrow">Content</p>
          <h3>Tree-first navigation</h3>
          <p>Path lookup, root listing, child listing, metadata edits, and value writes are available as incremental authoring slices.</p>
        </article>

        <article class="status-card">
          <p class="eyebrow">Safety</p>
          <h3>Preflight before delete</h3>
          <p>Dependency endpoints keep destructive flows explicit so the eventual UI can warn people before they regret a click.</p>
        </article>
      </section>

      <section class="roadmap-panel">
        <div>
          <p class="eyebrow">Next UI Moves</p>
          <h3>Admin work we can layer in without churn</h3>
        </div>

        <ul class="roadmap-list">
          <li>Template browser driven by the existing HAL-style template endpoints</li>
          <li>Content tree explorer backed by root and child listing contracts</li>
          <li>Read/write forms generated from template metadata instead of hardcoded screens</li>
        </ul>
      </section>
    </main>
  </div>
</template>
