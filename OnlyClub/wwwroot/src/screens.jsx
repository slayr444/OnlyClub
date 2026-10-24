import React, { useCallback, useEffect, useMemo, useRef, useState, memo } from 'react'
import { call } from './api'
import { hap, note, tgUser } from './tg'
import { money, fmt, dt, sleep, ST, BST, chipCls } from './utils'
import { Sheet, Ring, Qr, Btn } from './ui'

const rel = { position: 'relative', zIndex: 1 }
const ROLES = { Guest: 'Гость', Promoter: 'Промоутер', Guard: 'Охрана', Manager: 'Менеджер' }
const THEMES = [
    { id: 'theme-1', bg: 'linear-gradient(135deg, #3A36AF 0%, #2F2B9A 50%, #3B38A3 100%)' },
    { id: 'theme-2', bg: 'linear-gradient(135deg, #FBFAF8 0%, #EAEAF2 50%, #F4F5F0 100%)' },
    { id: 'theme-3', bg: 'linear-gradient(135deg, #682C52 0%, #8B0425 50%, #193889 100%)' },
    { id: 'theme-4', bg: 'linear-gradient(135deg, #262B63 0%, #27458D 50%, #355989 100%)' }
]

function useMounted() {
    const r = useRef(true)
    useEffect(() => {
        r.current = true
        return () => { r.current = false }
    }, [])
    return r
}

const HeroCard = memo(function HeroCard({ e, i, open }) {
    const click = useCallback(() => {
        hap()
        open(e)
    }, [e, open])
    return (
        <div className="glass hero press" style={{ '--i': i + 1 }} onClick={click}>
            <div className="row" style={rel}>
                <span className="chip ac">{e.minAge}+</span>
                <span className={'chip ' + (e.isClosed ? 'bad' : 'ok')}>{e.isClosed ? 'Закрыто' : 'Открыта продажа'}</span>
            </div>
            <h2 style={{ ...rel, fontSize: 30, marginTop: 42 }}>{e.title}</h2>
            <div className="sub" style={rel}>{fmt(e.startsAt)}</div>
            <div className="sub" style={rel}>Гостевой список · {e.guestListLimit} мест</div>
        </div>
    )
})

export function Home({ open }) {
    const [ev, setEv] = useState(null)
    const [err, setErr] = useState('')

    useEffect(() => {
        let off = false
        call('/events')
            .then(x => { if (!off) setEv(x) })
            .catch(x => { if (!off) setErr(x.message) })
        return () => { off = true }
    }, [])

    return (
        <div className="enter">
            <div style={{ '--i': 0 }}>
                <div className="sub" style={{ letterSpacing: '.3em', textTransform: 'uppercase', fontSize: 11 }}>Закрытый клуб</div>
                <h1 style={{ marginTop: 8 }}>
                    Сегодня ночь<br />
                    <i style={{ fontWeight: 400, color: 'var(--ac)' }}>принадлежит вам</i>
                </h1>
            </div>
            {err && <div className="glass lite card" style={{ '--i': 1 }}>{err}</div>}
            {!ev && !err && [1, 2].map(i => <div key={i} className="glass lite card skel" style={{ '--i': i, height: 150 }} />)}
            {ev?.map((e, i) => <HeroCard key={e.id} e={e} i={i} open={open} />)}
        </div>
    )
}

const TableCard = memo(function TableCard({ t, i, selected, closed, onPick }) {
    const st = ST[t.status]
    const click = useCallback(() => {
        if (t.status === 'Free' && !closed) onPick(t)
    }, [t, closed, onPick])
    return (
        <div
            className={'glass lite tbl press' + (selected ? ' sel' : '') + (t.status !== 'Free' || closed ? ' dis' : '')}
            style={{ '--i': i + 5 }}
            onClick={click}
        >
            <div className="row">
                <div className="num">{t.number}</div>
                <span style={{ fontSize: 12, color: st[1] }}>
                    <span className="dot" style={{ background: st[1] }} />
                    {st[0]}
                </span>
            </div>
            <div className="sub">до {t.capacity} гостей</div>
            <div style={{ marginTop: 8, fontSize: 13 }}>Мин. чек {money(t.minSpend)}</div>
            <div className="sub" style={{ marginTop: 2 }}>Депозит {money(t.deposit)}</div>
        </div>
    )
})

export function EventScreen({ e, back, onBook, toast }) {
    const [tb, setTb] = useState(null)
    const [sel, setSel] = useState(null)
    const [size, setSize] = useState(2)
    const [busy, setBusy] = useState(false)
    const [gl, setGl] = useState(false)
    const [name, setName] = useState(tgUser?.first_name || '')
    const [age, setAge] = useState('')
    const mounted = useMounted()

    const loadTables = useCallback(() => {
        return call(`/events/${e.id}/tables`)
            .then(x => { if (mounted.current) setTb(x) })
            .catch(x => { if (mounted.current) toast(x.message) })
    }, [e.id, toast, mounted])

    useEffect(() => { loadTables() }, [loadTables])

    const pick = useCallback(t => {
        hap()
        setSel(t)
        setSize(Math.min(2, t.capacity))
    }, [])

    const closeSel = useCallback(() => setSel(null), [])
    const closeGl = useCallback(() => setGl(false), [])
    const openGl = useCallback(() => setGl(true), [])
    const goBack = useCallback(() => {
        hap()
        back()
    }, [back])
    const dec = useCallback(() => {
        hap()
        setSize(s => Math.max(1, s - 1))
    }, [])
    const inc = useCallback(() => {
        hap()
        setSize(s => Math.min(sel ? sel.capacity : s, s + 1))
    }, [sel])

    const book = async () => {
        setBusy(true)
        try {
            const b = await call('/bookings', { method: 'POST', body: { eventTableId: sel.id, partySize: size } })
            note('success')
            onBook({ ...b, deposit: sel.deposit, title: e.title })
        } catch (x) {
            note('error')
            toast(x.message)
            if (mounted.current) {
                setSel(null)
                loadTables()
            }
        }
        if (mounted.current) setBusy(false)
    }

    const reg = async () => {
        if (+age < e.minAge) {
            note('error')
            toast(`Вход только с ${e.minAge} лет!`)
            return
        }
        if (+age > 60) {
            note('error')
            toast('Максимальный возраст для входа — 60 лет')
            return
        }

        setBusy(true)
        try {
            await call(`/events/${e.id}/guestlist`, { method: 'POST', body: { name, age: +age, parentEntryId: null } })
            note('success')
            toast('Вы в списке ✓')
            if (mounted.current) setGl(false)
        } catch (x) {
            note('error')
            toast(x.message)
        }
        if (mounted.current) setBusy(false)
    }

    const ageBad = !name || !age || +age < e.minAge || +age > 60

    return (
        <div className="enter">
            <div style={{ '--i': 0 }}>
                <div className="glass lite back press" onClick={goBack}>‹ Назад</div>
            </div>
            <h1 style={{ '--i': 1 }}>{e.title}</h1>
            <div className="sub" style={{ '--i': 2 }}>{fmt(e.startsAt)} · {e.minAge}+</div>
            <div style={{ '--i': 3, marginTop: 16 }}>
                <Btn ghost dis={e.isClosed} onClick={openGl}>Встать в гостевой список</Btn>
            </div>
            <h2 style={{ '--i': 4, marginTop: 28 }}>Столы</h2>
            <div className="grid">
                {!tb
                    ? [1, 2, 3, 4].map(i => <div key={i} className="glass lite tbl skel" style={{ '--i': i + 4, height: 130 }} />)
                    : tb.map((t, i) => (
                        <TableCard key={t.id} t={t} i={i} selected={sel?.id === t.id} closed={e.isClosed} onPick={pick} />
                    ))}
            </div>

            {sel && (
                <Sheet onClose={closeSel}>
                    <div className="row">
                        <h2>Стол {sel.number}</h2>
                        <span className="chip ac">Депозит {money(sel.deposit)}</span>
                    </div>
                    <div className="row" style={{ margin: '20px 0' }}>
                        <span className="sub" style={{ margin: 0 }}>Гостей</span>
                        <div className="step">
                            <div className="glass lite rnd press" onClick={dec}>−</div>
                            <b key={size}>{size}</b>
                            <div className="glass lite rnd press" onClick={inc}>+</div>
                        </div>
                    </div>
                    <Btn busy={busy} onClick={book}>Забронировать на 15 минут</Btn>
                </Sheet>
            )}

            {gl && (
                <Sheet onClose={closeGl}>
                    <h2>Гостевой список</h2>
                    <div className="sub">Возраст: от {e.minAge} до 60 лет</div>
                    <input className="field" placeholder="Имя" value={name} onChange={x => setName(x.target.value)} />
                    <input className="field" placeholder="Возраст" inputMode="numeric" value={age} onChange={x => setAge(x.target.value.replace(/\D/g, ''))} />

                    {age && +age < e.minAge && (
                        <div className="sub" style={{ color: 'var(--bad)', marginTop: 8, textAlign: 'center' }}>
                            Вход только с {e.minAge} лет
                        </div>
                    )}
                    {age && +age > 60 && (
                        <div className="sub" style={{ color: 'var(--bad)', marginTop: 8, textAlign: 'center' }}>
                            Вход до 60 лет
                        </div>
                    )}

                    <div style={{ marginTop: 16 }}>
                        <Btn busy={busy} dis={ageBad} onClick={reg}>Записаться</Btn>
                    </div>
                </Sheet>
            )}
        </div>
    )
}

export function Booking({ b, done, toast }) {
    const [cur, setCur] = useState(b)
    const [busy, setBusy] = useState(false)
    const [exp, setExp] = useState(false)
    const mounted = useMounted()

    useEffect(() => {
        if (cur.status !== 'Held') return
        const ms = dt(cur.heldUntilAt) - Date.now()
        if (ms <= 0) {
            setExp(true)
            return
        }
        const id = setTimeout(() => setExp(true), ms + 50)
        return () => clearTimeout(id)
    }, [cur.status, cur.heldUntilAt])

    const pay = async () => {
        setBusy(true)
        try {
            const r = await call(`/bookings/${cur.id}/pay`, { method: 'POST' })
            if (r && r.success === false) throw new Error('Платёж не прошёл')
            for (let i = 0; i < 20; i++) {
                if (!mounted.current) return
                const list = await call('/bookings/my')
                const n = list.find(x => x.id === cur.id)
                if (n && n.status === 'Confirmed') {
                    note('success')
                    if (mounted.current) {
                        setCur(c => ({ ...c, ...n }))
                        setBusy(false)
                    }
                    return
                }
                await sleep(1500)
            }
            toast('Оплата обрабатывается, проверьте брони чуть позже')
        } catch (x) {
            note('error')
            toast(x.message)
        }
        if (mounted.current) setBusy(false)
    }

    const ok = cur.status === 'Confirmed'

    return (
        <div className="enter" style={{ textAlign: 'center' }}>
            <div style={{ '--i': 0, textAlign: 'left' }}>
                <div className="glass lite back press" onClick={done}>‹ К броням</div>
            </div>
            <h1 style={{ '--i': 1 }}>{ok ? 'Стол ваш' : exp ? 'Время вышло' : 'Оплатите депозит'}</h1>
            <div className="sub" style={{ '--i': 2 }}>
                {cur.title ? cur.title + ' · ' : ''}Стол {cur.tableNumber} · {cur.partySize} гост.
            </div>
            {ok ? (
                <div style={{ '--i': 3 }}>
                    <Qr token={cur.qrToken} />
                    <div className="sub">Покажите код охране на входе</div>
                </div>
            ) : (
                <div className="glass lite card" style={{ '--i': 3, display: 'flex', alignItems: 'center', gap: 18, textAlign: 'left' }}>
                    <Ring until={cur.heldUntilAt} />
                    <div>
                        <div className="sub" style={{ margin: 0 }}>Депозит</div>
                        <div className="serif" style={{ fontSize: 30, fontWeight: 700 }}>{cur.deposit ? money(cur.deposit) : 'обязателен'}</div>
                        <div className="sub">Зачтётся в минимальный чек</div>
                    </div>
                </div>
            )}
            {!ok && (
                <div style={{ '--i': 4, marginTop: 18 }}>
                    <Btn busy={busy} dis={exp} onClick={pay}>{exp ? 'Бронь истекла' : cur.deposit ? 'Оплатить ' + money(cur.deposit) : 'Оплатить'}</Btn>
                </div>
            )}
        </div>
    )
}

const BookingCard = memo(function BookingCard({ b, i, onOpen, onQr, onCancel }) {
    return (
        <div className="glass lite card" style={{ '--i': i + 1 }}>
            <div className="row">
                <h2>Стол {b.tableNumber}</h2>
                <span className={'chip ' + chipCls(b.status)}>{BST[b.status]}</span>
            </div>
            <div className="sub">{b.partySize} гост.</div>
            <div style={{ marginTop: 10, fontSize: 14 }}>
                Депозит внесён: {money(b.depositPaid)} · остаток чека {money(b.remainingMinSpend)}
            </div>
            {b.status === 'Held' && (
                <div style={{ marginTop: 14 }}><Btn onClick={() => onOpen(b)}>Оплатить</Btn></div>
            )}
            {b.status === 'Confirmed' && (
                <div className="row" style={{ marginTop: 14 }}>
                    <Btn onClick={() => onQr(b)}>QR-код</Btn>
                    <Btn ghost onClick={() => onCancel(b)}>Отменить</Btn>
                </div>
            )}
        </div>
    )
})

export function Bookings({ open, toast }) {
    const [l, setL] = useState(null)
    const [qr, setQr] = useState(null)
    const [cx, setCx] = useState(null)
    const mounted = useMounted()

    const load = useCallback(() => {
        return call('/bookings/my')
            .then(x => { if (mounted.current) setL(x) })
            .catch(x => {
                if (!mounted.current) return
                setL([])
                toast(x.message)
            })
    }, [toast, mounted])

    useEffect(() => { load() }, [load])

    const cancel = async b => {
        try {
            const r = await call(`/bookings/${b.id}/cancel`, { method: 'POST' })
            note('warning')
            toast(r?.refundAmount > 0 ? 'Возврат ' + money(r.refundAmount) : 'Бронь отменена')
            if (mounted.current) {
                setCx(null)
                load()
            }
        } catch (x) {
            toast(x.message)
        }
    }

    const closeQr = useCallback(() => setQr(null), [])
    const closeCx = useCallback(() => setCx(null), [])

    return (
        <div className="enter">
            <h1 style={{ '--i': 0 }}>Мои брони</h1>
            {l && !l.length && (
                <div className="glass lite card" style={{ '--i': 1, textAlign: 'center' }}>
                    <h2>Пока пусто</h2>
                    <div className="sub">Выберите событие в афише</div>
                </div>
            )}
            {!l && <div className="glass lite card skel" style={{ '--i': 1, height: 110 }} />}
            {l?.map((b, i) => (
                <BookingCard key={b.id} b={b} i={i} onOpen={open} onQr={setQr} onCancel={setCx} />
            ))}
            {qr && (
                <Sheet onClose={closeQr}>
                    <h2 style={{ textAlign: 'center' }}>Стол {qr.tableNumber}</h2>
                    <Qr token={qr.qrToken} />
                </Sheet>
            )}
            {cx && (
                <Sheet onClose={closeCx}>
                    <h2>Отменить бронь?</h2>
                    <div className="sub" style={{ marginBottom: 16 }}>Возврат: 100% за 48 ч, 50% за 12 ч, позже — без возврата.</div>
                    <Btn onClick={() => cancel(cx)}>Да, отменить</Btn>
                    <div style={{ height: 10 }} />
                    <Btn ghost onClick={closeCx}>Оставить</Btn>
                </Sheet>
            )}
        </div>
    )
}

const GuestCard = memo(function GuestCard({ e, i, onShow }) {
    return (
        <div className="glass lite card press" style={{ '--i': i + 1 }} onClick={() => onShow(e)}>
            <div className="row">
                <h2>{e.name}</h2>
                <span className={'chip ' + chipCls(e.status)}>{(ST[e.status] || [e.status])[0]}</span>
            </div>
            <div className="sub">Нажмите, чтобы показать QR</div>
        </div>
    )
})

export function GuestList({ toast }) {
    const [l, setL] = useState(null)
    const [qr, setQr] = useState(null)
    const [cx, setCx] = useState(null)
    const mounted = useMounted()

    const load = useCallback(() => {
        return call('/guestlist/my')
            .then(x => { if (mounted.current) setL(x) })
            .catch(() => { if (mounted.current) setL([]) })
    }, [mounted])

    useEffect(() => { load() }, [load])

    const show = useCallback(async e => {
        hap()
        try {
            const r = await call(`/guestlist/${e.id}/qr`)
            if (mounted.current) setQr({ id: e.id, name: e.name, token: typeof r === 'string' ? r : r.qr || r.token })
        } catch { }
    }, [mounted])

    const cancel = async entryId => {
        try {
            await call(`/guestlist/${entryId}/cancel`, { method: 'POST' })
            note('success')
            toast('Заявка отменена')
            if (mounted.current) {
                setCx(null)
                setQr(null)
                load()
            }
        } catch (x) {
            toast(x.message)
        }
    }

    const closeQr = useCallback(() => setQr(null), [])
    const closeCx = useCallback(() => setCx(null), [])

    return (
        <div className="enter">
            <h1 style={{ '--i': 0 }}>
                Гостевой<br />
                <i style={{ fontWeight: 400, color: 'var(--ac)' }}>список</i>
            </h1>
            {!l && <div className="glass lite card skel" style={{ '--i': 1, height: 90 }} />}
            {l && !l.length && (
                <div className="glass lite card" style={{ '--i': 1, textAlign: 'center' }}>
                    <h2>Вы пока не записаны</h2>
                    <div className="sub">Откройте событие и нажмите «Встать в список»</div>
                </div>
            )}
            {l?.map((e, i) => <GuestCard key={e.id} e={e} i={i} onShow={show} />)}
            {qr && (
                <Sheet onClose={closeQr}>
                    <h2 style={{ textAlign: 'center' }}>{qr.name}</h2>
                    <Qr token={qr.token} />
                    <div style={{ marginTop: 24 }}>
                        <Btn ghost onClick={() => { setCx(qr); setQr(null) }}>Отменить заявку</Btn>
                    </div>
                </Sheet>
            )}

            {cx && (
                <Sheet onClose={closeCx}>
                    <h2>Отменить заявку?</h2>
                    <div className="sub" style={{ marginBottom: 16 }}>Вы уверены, что хотите выписаться из гостевого списка на это событие?</div>
                    <Btn onClick={() => cancel(cx.id)}>Да, отменить</Btn>
                    <div style={{ height: 10 }} />
                    <Btn ghost onClick={closeCx}>Оставить</Btn>
                </Sheet>
            )}
        </div>
    )
}

const Swatch = memo(function Swatch({ t, active, set }) {
    return (
        <div
            className={'theme-swatch' + (active ? ' active' : '')}
            style={{ background: t.bg }}
            onClick={() => {
                hap()
                set(t.id)
            }}
        />
    )
})

export function Profile({ theme, setTheme }) {
    const [m, setM] = useState({ name: tgUser?.first_name || 'Гость', telegramId: tgUser?.id || 0, role: 'Guest' })

    useEffect(() => {
        let off = false
        call('/me')
            .then(x => { if (!off) setM(p => ({ ...p, ...x })) })
            .catch(() => { })
        return () => { off = true }
    }, [])

    const rows = useMemo(() => [
        ['Роль', ROLES[m.role] || m.role],
        ['Депозиты', 'Зачитываются в чек'],
        ['Удержание стола', '15 минут'],
        ['Вход', 'По QR-коду']
    ], [m.role])

    return (
        <div className="enter">
            <div className="glass hero" style={{ '--i': 0, textAlign: 'center' }}>
                <div className="glass avatar">{(m.name || '?')[0]}</div>
                <h2 style={{ ...rel, marginTop: 16 }}>{m.name}</h2>
                <div className="sub" style={rel}>ID {m.telegramId || '—'}</div>
            </div>

            {rows.map(([a, b], i) => (
                <div key={a} className="glass lite card row" style={{ '--i': i + 1 }}>
                    <span className="sub" style={{ margin: 0 }}>{a}</span>
                    <b className="serif">{b}</b>
                </div>
            ))}

            <div className="glass lite card" style={{ '--i': 5 }}>
                <div className="row" style={{ marginBottom: 8 }}>
                    <span className="sub" style={{ margin: 0 }}>Цвет фона</span>
                </div>
                <div className="theme-swatches">
                    {THEMES.map(t => <Swatch key={t.id} t={t} active={theme === t.id} set={setTheme} />)}
                </div>
            </div>
        </div>
    )
}