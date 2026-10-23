import React, { useEffect, useState } from 'react'
import { call } from './api'
import { hap, note, tgUser } from './tg'
import { money, fmt, dt, sleep, ST, BST, chipCls } from './utils'
import { Sheet, Ring, Qr, Btn } from './ui'

const rel = { position: 'relative', zIndex: 1 }

export function Home({ open }) {
    const [ev, setEv] = useState(null)
    const [err, setErr] = useState('')
    useEffect(() => { call('/events').then(setEv).catch(x => setErr(x.message)) }, [])
    return (
        <div className="enter">
            <div style={{ '--i': 0 }}>
                <div className="sub" style={{ letterSpacing: '.3em', textTransform: 'uppercase', fontSize: 11 }}>Закрытый клуб</div>
                <h1 style={{ marginTop: 8 }}>
                    Сегодня ночь<br />
                    <i style={{ fontWeight: 400, color: 'var(--ac)' }}>принадлежит вам</i>
                </h1>
            </div>
            {err && <div className="glass card" style={{ '--i': 1 }}>{err}</div>}
            {!ev && !err && [1, 2].map(i => <div key={i} className="glass card skel" style={{ '--i': i, height: 150 }} />)}
            {ev?.map((e, i) => (
                <div key={e.id} className="glass hero press" style={{ '--i': i + 1 }} onClick={() => { hap(); open(e) }}>
                    <div className="row" style={rel}>
                        <span className="chip ac">{e.minAge}+</span>
                        <span className={'chip ' + (e.isClosed ? 'bad' : 'ok')}>{e.isClosed ? 'Закрыто' : 'Открыта продажа'}</span>
                    </div>
                    <h2 style={{ ...rel, fontSize: 30, marginTop: 42 }}>{e.title}</h2>
                    <div className="sub" style={rel}>{fmt(e.startsAt)}</div>
                    <div className="sub" style={rel}>Гостевой список · {e.guestListLimit} мест</div>
                </div>
            ))}
        </div>
    )
}

export function EventScreen({ e, back, onBook, toast }) {
    const [tb, setTb] = useState(null)
    const [sel, setSel] = useState(null)
    const [size, setSize] = useState(2)
    const [busy, setBusy] = useState(false)
    const [gl, setGl] = useState(false)
    const [name, setName] = useState(tgUser?.first_name || '')
    const [age, setAge] = useState('')

    const loadTables = () => call(`/events/${e.id}/tables`).then(setTb).catch(x => toast(x.message))
    useEffect(() => { loadTables() }, [e.id])

    const pick = t => { hap(); setSel(t); setSize(Math.min(2, t.capacity)) }

    const book = async () => {
        setBusy(true)
        try {
            const b = await call('/bookings', { method: 'POST', body: { eventTableId: sel.id, partySize: size } })
            note('success')
            onBook({ ...b, deposit: sel.deposit, title: e.title })
        } catch (x) {
            note('error')
            toast(x.message)
            setSel(null)
            loadTables()
        }
        setBusy(false)
    }

    const reg = async () => {
        setBusy(true)
        try {
            await call(`/events/${e.id}/guestlist`, { method: 'POST', body: { name, age: +age, parentEntryId: null } })
            note('success')
            toast('Вы в списке ✓')
            setGl(false)
        } catch (x) {
            note('error')
            toast(x.message)
        }
        setBusy(false)
    }

    return (
        <div className="enter">
            <div style={{ '--i': 0 }}>
                <div className="glass back press" onClick={() => { hap(); back() }}>‹ Назад</div>
            </div>
            <h1 style={{ '--i': 1 }}>{e.title}</h1>
            <div className="sub" style={{ '--i': 2 }}>{fmt(e.startsAt)} · {e.minAge}+</div>
            <div style={{ '--i': 3, marginTop: 16 }}>
                <Btn ghost dis={e.isClosed} onClick={() => setGl(true)}>Встать в гостевой список</Btn>
            </div>
            <h2 style={{ '--i': 4, marginTop: 28 }}>Столы</h2>
            <div className="grid">
                {!tb
                    ? [1, 2, 3, 4].map(i => <div key={i} className="glass tbl skel" style={{ '--i': i + 4, height: 130 }} />)
                    : tb.map((t, i) => (
                        <div
                            key={t.id}
                            className={'glass tbl press' + (sel?.id === t.id ? ' sel' : '') + (t.status !== 'Free' || e.isClosed ? ' dis' : '')}
                            style={{ '--i': i + 5 }}
                            onClick={() => pick(t)}
                        >
                            <div className="row">
                                <div className="num">{t.number}</div>
                                <span style={{ fontSize: 12, color: ST[t.status][1] }}>
                                    <span className="dot" style={{ background: ST[t.status][1] }} />
                                    {ST[t.status][0]}
                                </span>
                            </div>
                            <div className="sub">до {t.capacity} гостей</div>
                            <div style={{ marginTop: 8, fontSize: 13 }}>Мин. чек {money(t.minSpend)}</div>
                            <div className="sub" style={{ marginTop: 2 }}>Депозит {money(t.deposit)}</div>
                        </div>
                    ))}
            </div>

            {sel && (
                <Sheet onClose={() => setSel(null)}>
                    <div className="row">
                        <h2>Стол {sel.number}</h2>
                        <span className="chip ac">Депозит {money(sel.deposit)}</span>
                    </div>
                    <div className="row" style={{ margin: '20px 0' }}>
                        <span className="sub" style={{ margin: 0 }}>Гостей</span>
                        <div className="step">
                            <div className="glass rnd press" onClick={() => { hap(); setSize(Math.max(1, size - 1)) }}>−</div>
                            <b key={size}>{size}</b>
                            <div className="glass rnd press" onClick={() => { hap(); setSize(Math.min(sel.capacity, size + 1)) }}>+</div>
                        </div>
                    </div>
                    <Btn busy={busy} onClick={book}>Забронировать на 15 минут</Btn>
                </Sheet>
            )}

            {gl && (
                <Sheet onClose={() => setGl(false)}>
                    <h2>Гостевой список</h2>
                    <div className="sub">Минимальный возраст — {e.minAge}</div>
                    <input className="field" placeholder="Имя" value={name} onChange={x => setName(x.target.value)} />
                    <input className="field" placeholder="Возраст" inputMode="numeric" value={age} onChange={x => setAge(x.target.value.replace(/\D/g, ''))} />
                    <div style={{ marginTop: 16 }}>
                        <Btn busy={busy} dis={!name || !age} onClick={reg}>Записаться</Btn>
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

    useEffect(() => {
        if (cur.status !== 'Held') return
        const i = setInterval(() => {
            if (dt(cur.heldUntilAt) < Date.now()) { setExp(true); clearInterval(i) }
        }, 500)
        return () => clearInterval(i)
    }, [cur])

    const pay = async () => {
        setBusy(true)
        try {
            const r = await call(`/bookings/${cur.id}/pay`, { method: 'POST' })
            if (r && r.success === false) throw new Error('Платёж не прошёл')
            for (let i = 0; i < 20; i++) {
                const list = await call('/bookings/my')
                const n = list.find(x => x.id === cur.id)
                if (n && n.status === 'Confirmed') {
                    note('success')
                    setCur({ ...cur, ...n })
                    setBusy(false)
                    return
                }
                await sleep(1500)
            }
            toast('Оплата обрабатывается, проверьте брони чуть позже')
        } catch (x) {
            note('error')
            toast(x.message)
        }
        setBusy(false)
    }

    const ok = cur.status === 'Confirmed'

    return (
        <div className="enter" style={{ textAlign: 'center' }}>
            <div style={{ '--i': 0, textAlign: 'left' }}>
                <div className="glass back press" onClick={done}>‹ К броням</div>
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
                <div className="glass card" style={{ '--i': 3, display: 'flex', alignItems: 'center', gap: 18, textAlign: 'left' }}>
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

export function Bookings({ open, toast }) {
    const [l, setL] = useState(null)
    const [qr, setQr] = useState(null)
    const [cx, setCx] = useState(null)
    const load = () => call('/bookings/my').then(setL).catch(x => { setL([]); toast(x.message) })
    useEffect(() => { load() }, [])

    const cancel = async b => {
        try {
            const r = await call(`/bookings/${b.id}/cancel`, { method: 'POST' })
            note('warning')
            toast(r?.refundAmount > 0 ? 'Возврат ' + money(r.refundAmount) : 'Бронь отменена')
            setCx(null)
            load()
        } catch (x) {
            toast(x.message)
        }
    }

    return (
        <div className="enter">
            <h1 style={{ '--i': 0 }}>Мои брони</h1>
            {l && !l.length && (
                <div className="glass card" style={{ '--i': 1, textAlign: 'center' }}>
                    <h2>Пока пусто</h2>
                    <div className="sub">Выберите событие в афише</div>
                </div>
            )}
            {!l && <div className="glass card skel" style={{ '--i': 1, height: 110 }} />}
            {l?.map((b, i) => (
                <div key={b.id} className="glass card" style={{ '--i': i + 1 }}>
                    <div className="row">
                        <h2>Стол {b.tableNumber}</h2>
                        <span className={'chip ' + chipCls(b.status)}>{BST[b.status]}</span>
                    </div>
                    <div className="sub">{b.partySize} гост.</div>
                    <div style={{ marginTop: 10, fontSize: 14 }}>
                        Депозит внесён: {money(b.depositPaid)} · остаток чека {money(b.remainingMinSpend)}
                    </div>
                    {b.status === 'Held' && (
                        <div style={{ marginTop: 14 }}><Btn onClick={() => open(b)}>Оплатить</Btn></div>
                    )}
                    {b.status === 'Confirmed' && (
                        <div className="row" style={{ marginTop: 14 }}>
                            <Btn onClick={() => setQr(b)}>QR-код</Btn>
                            <Btn ghost onClick={() => setCx(b)}>Отменить</Btn>
                        </div>
                    )}
                </div>
            ))}
            {qr && (
                <Sheet onClose={() => setQr(null)}>
                    <h2 style={{ textAlign: 'center' }}>Стол {qr.tableNumber}</h2>
                    <Qr token={qr.qrToken} />
                </Sheet>
            )}
            {cx && (
                <Sheet onClose={() => setCx(null)}>
                    <h2>Отменить бронь?</h2>
                    <div className="sub" style={{ marginBottom: 16 }}>Возврат: 100% за 48 ч, 50% за 12 ч, позже — без возврата.</div>
                    <Btn onClick={() => cancel(cx)}>Да, отменить</Btn>
                    <div style={{ height: 10 }} />
                    <Btn ghost onClick={() => setCx(null)}>Оставить</Btn>
                </Sheet>
            )}
        </div>
    )
}

export function GuestList() {
    const [l, setL] = useState(null)
    const [qr, setQr] = useState(null)
    useEffect(() => { call('/guestlist/my').then(setL).catch(() => setL([])) }, [])

    const show = async e => {
        hap()
        try {
            const r = await call(`/guestlist/${e.id}/qr`)
            setQr({ name: e.name, token: typeof r === 'string' ? r : r.qr || r.token })
        } catch { }
    }

    return (
        <div className="enter">
            <h1 style={{ '--i': 0 }}>
                Гостевой<br />
                <i style={{ fontWeight: 400, color: 'var(--ac)' }}>список</i>
            </h1>
            {!l && <div className="glass card skel" style={{ '--i': 1, height: 90 }} />}
            {l && !l.length && (
                <div className="glass card" style={{ '--i': 1, textAlign: 'center' }}>
                    <h2>Вы пока не записаны</h2>
                    <div className="sub">Откройте событие и нажмите «Встать в список»</div>
                </div>
            )}
            {l?.map((e, i) => (
                <div key={e.id} className="glass card press" style={{ '--i': i + 1 }} onClick={() => show(e)}>
                    <div className="row">
                        <h2>{e.name}</h2>
                        <span className={'chip ' + chipCls(e.status)}>{(ST[e.status] || [e.status])[0]}</span>
                    </div>
                    <div className="sub">Нажмите, чтобы показать QR</div>
                </div>
            ))}
            {qr && (
                <Sheet onClose={() => setQr(null)}>
                    <h2 style={{ textAlign: 'center' }}>{qr.name}</h2>
                    <Qr token={qr.token} />
                </Sheet>
            )}
        </div>
    )
}

const ROLES = { Guest: 'Гость', Promoter: 'Промоутер', Guard: 'Охрана', Manager: 'Менеджер' }

export function Profile() {
    const [m, setM] = useState({ name: tgUser?.first_name || 'Гость', telegramId: tgUser?.id || 0, role: 'Guest' })
    useEffect(() => { call('/me').then(x => setM(p => ({ ...p, ...x }))).catch(() => { }) }, [])
    return (
        <div className="enter">
            <div className="glass hero" style={{ '--i': 0, textAlign: 'center' }}>
                <div
                    className="glass"
                    style={{ width: 92, height: 92, borderRadius: '50%', margin: '0 auto', display: 'grid', placeItems: 'center', fontFamily: 'var(--serif)', fontSize: 42, fontWeight: 700, animation: 'pulse 2s infinite' }}
                >
                    {(m.name || '?')[0]}
                </div>
                <h2 style={{ ...rel, marginTop: 16 }}>{m.name}</h2>
                <div className="sub" style={rel}>ID {m.telegramId || '—'}</div>
            </div>
            {[['Роль', ROLES[m.role] || m.role], ['Депозиты', 'Зачитываются в чек'], ['Удержание стола', '15 минут'], ['Вход', 'По QR-коду']].map(([a, b], i) => (
                <div key={a} className="glass card row" style={{ '--i': i + 1 }}>
                    <span className="sub" style={{ margin: 0 }}>{a}</span>
                    <b className="serif">{b}</b>
                </div>
            ))}
        </div>
    )
}