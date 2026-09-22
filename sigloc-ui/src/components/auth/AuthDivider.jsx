/* eslint-disable react/prop-types */

/** Horizontal "ou" separator used between the credential form and Google auth. */
export function AuthDivider({ label = "ou" }) {
  return (
    <div className="flex items-center gap-3">
      <span className="h-px flex-1 bg-slate-200" />
      <span className="text-xs font-medium uppercase text-slate-400">{label}</span>
      <span className="h-px flex-1 bg-slate-200" />
    </div>
  )
}

export default AuthDivider
