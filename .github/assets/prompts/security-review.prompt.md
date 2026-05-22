---
description: Security review via parallel sub-agent analysis. Main agent orchestrates only—does NOT perform detailed code analysis. Each security theme analyzed by dedicated sub-agent.
agent: agent
tools: [read,search,execute,todo,agent]
---

# Security Review — Sub-Agent Orchestrated

**CRITICAL RULE:** This is an orchestration task. You are the conductor, NOT the analyst.

- ✅ Your job: Scope determination, sub-agent spawning, result collection, final report
- ❌ You do NOT: Read code, identify vulnerabilities, filter findings, score confidence

All detailed analysis is delegated to parallel sub-agents.

---

## Step 1 — Determine Scope

Run (you do this):
```bash
git diff --merge-base origin/HEAD
```

If output exists → review scope is **branch diff**.  
If no output → review scope is **full `src/` directory**.

Record the scope clearly.

---

## Step 2 — Spawn 4 Parallel Sub-Agents (ONE for each theme)

Do NOT analyze code yourself. Instead, spawn exactly 4 sub-agents in parallel, one per security theme:

### Sub-Agent 1: Input & Injection Theme

**Prompt:**
> Review the beneficiaries feature code scope [FILES] for input & injection vulnerabilities:
> - SQL injection via EF Core raw queries (`FromSqlRaw`, `ExecuteSqlRaw`)
> - Command injection in `Process.Start` or shell invocations
> - Path traversal in file operations
> - XSS via unescaped Razor output (`@Html.Raw`, `[AllowHtml]`)
>
> For EACH candidate finding: record file, line, category, description, exploit scenario.
> Return as structured list: `[{file, line, description, scenario}]`

### Sub-Agent 2: Auth & Authorization Theme

**Prompt:**
> Review the beneficiaries feature code scope [FILES] for auth & authz vulnerabilities:
> - Missing `[Authorize]` on controller actions accessing user data
> - Broken object-level auth — account/transaction/beneficiary access not filtered by `UserId`
> - Authentication bypass logic
> - CSRF — POST forms missing `@Html.AntiForgeryToken()` or `[ValidateAntiForgeryToken]`
>
> For EACH candidate finding: record file, line, category, description, exploit scenario.
> Return as structured list: `[{file, line, description, scenario}]`

### Sub-Agent 3: Data Exposure Theme

**Prompt:**
> Review the beneficiaries feature code scope [FILES] for data exposure vulnerabilities:
> - Sensitive data (passwords, tokens, PII) written to logs
> - Stack traces or debug info returned to the browser
> - Over-exposed API responses (returning full entity instead of DTO)
>
> For EACH candidate finding: record file, line, category, description, exploit scenario.
> Return as structured list: `[{file, line, description, scenario}]`

### Sub-Agent 4: Crypto & Secrets Theme

**Prompt:**
> Review the beneficiaries feature code scope [FILES] for crypto & secrets vulnerabilities:
> - Hardcoded credentials or connection strings in source
> - Weak or missing password policy enforcement
> - Insecure data protection key storage
>
> For EACH candidate finding: record file, line, category, description, exploit scenario.
> Return as structured list: `[{file, line, description, scenario}]`

---

## Step 3 — Collect Results from All 4 Sub-Agents

Wait for all 4 sub-agents to return their findings.

Aggregate all candidate findings into a single list (from all themes).

---

## Step 4 — Spawn N Parallel Filter Sub-Agents (ONE per candidate)

For EACH candidate vulnerability from Step 3, spawn a dedicated sub-agent:

**Prompt (per candidate):**
> **Task:** Evaluate a candidate vulnerability against exclusion criteria.
>
> **Finding:** [candidate description + file:line]
>
> **Evaluate against auto-exclusions:**
> 1. Denial of Service or resource exhaustion
> 2. Rate limiting concerns
> 3. Missing input validation on non-security-critical fields
> 4. Memory safety issues (C# is memory-safe — no buffer overflows)
> 5. Theoretical race conditions without a concrete attack path
> 6. Outdated NuGet packages (managed separately)
> 7. Findings only in test files (`tests/`)
> 8. Log spoofing (non-PII data in logs is not a vulnerability)
> 9. SSRF that controls only the URL path, not the host
> 10. Lack of audit logs
> 11. Missing hardening measures without a concrete exploit path
> 12. Documentation files (`.md`, `.txt`)
> 13. Data and approaches related to workshop/demo application like  hardcoded demonstration data
>
> **Then assign confidence score 1–10:**
> - 8–10: Clear exploit path, concrete and actionable → INCLUDE
> - 5–7: Possible issue requiring specific conditions → EXCLUDE
> - 1–4: Theoretical or speculative → EXCLUDE
>
> **Return:** `{finding, file, line, confidence_score (int), include_exclude_decision, reasoning}`

Spawn all filter sub-agents in parallel.

---

## Step 5 — Collect Filtered Results

Collect all filter sub-agent responses.

Keep only findings where:
- `confidence_score >= 8`
- `include_exclude_decision == INCLUDE`

Discard all others.

---

## Step 6 — Generate Final Report

Output markdown report with only surviving findings:

```
## Security Review — [today's date]
**Scope:** [branch diff / full repo]  
**Reviewed feature:** [e.g., beneficiaries]

### Finding 1: [CATEGORY] — `[file]:[line]`
- **Severity:** HIGH / MEDIUM / LOW
- **Description:** [what the vulnerability is]
- **Exploit scenario:** [how an attacker triggers it]
- **Recommendation:** [specific fix]

### Finding 2: ...

---

If no findings survive the filter:
> ✅ No high-confidence vulnerabilities found in scope.
```

---

## CHECKLIST — Ensure Sub-Agent Usage

Before submitting report, verify:
- [ ] Step 1: Did you determine scope (git diff or full `src/`)?
- [ ] Step 2: Did you spawn exactly 4 parallel sub-agents (Input, Auth, Data, Crypto)?
- [ ] Step 3: Did you collect results from all 4 sub-agents?
- [ ] Step 4: Did you spawn 1 filter sub-agent per candidate finding?
- [ ] Step 5: Did you filter by confidence >= 8 and INCLUDE decision only?
- [ ] Step 6: Did you generate markdown report with surviving findings only?

**FAIL if you analyzed code yourself.** Sub-agents do the analysis.
