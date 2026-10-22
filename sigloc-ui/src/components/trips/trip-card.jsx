/** B2B information card: fixed-height header with a colored icon, uppercase title and an optional right-side slot. */
export function TripCard({ icon: Icon, iconClassName = "text-slate-500", title, aside, className = "", bodyClassName = "p-4", children }) {
  return (
    <section className={`flex flex-col rounded-xl border border-slate-200 bg-white ${className}`}>
      <header className="flex h-[52px] shrink-0 items-center gap-2 rounded-t-xl border-b border-slate-100 bg-slate-50/50 px-4">
        <Icon size={16} className={iconClassName} />
        <h3 className="text-xs font-bold uppercase text-slate-700">{title}</h3>
        {aside && <div className="ml-auto flex items-center gap-2">{aside}</div>}
      </header>
      <div className={`flex-1 ${bodyClassName}`}>{children}</div>
    </section>
  )
}
