import { dt } from './utils'
import React, { useEffect, useRef, useState, memo } from 'react'
import { createPortal } from 'react-dom'
import qrcode from 'qrcode-generator'
import { hap } from './tg'

const I = {
    home: 'M4 11l8-7 8 7v9a1 1 0 01-1 1h-4v-6H9v6H5a1 1 0 01-1-1z',
    list: 'M8 6h12M8 12h12M8 18h12M4 6h.01M4 12h.01M4 18h.01',
    tick: 'M3 9a2 2 0 002-2h14a2 2 0 002 2v1a2 2 0 000 4v1a2 2 0 00-2 2H5a2 2 0 00-2-2v-1a2 2 0 000-4z M14 7v10',
    user: 'M12 12a4 4 0 100-8 4 4 0 000 8zM4 21a8 8 0 0116 0'
}

const Ico = memo(({ d }) => (
    <svg viewBox="0 0 24 24">
        <path d={d} />
    </svg>
))

const TABS = [['home', 'Афиша'], ['list', 'Список'], ['tick', 'Брони'], ['user', 'Профиль']]

export const Tabs = memo(function Tabs({ tab, set }) {
    const i = TABS.findIndex(x => x[0] === tab)
    return (
        <nav className="bar">
            <div className="pill" style={{ transform: `translate3d(${i * 100}%,0,0)` }} />
            {TABS.map(([k, l]) => (
                <div
                    key={k}
                    className={'tab' + (tab === k ? ' on' : '')}
                    onClick={() => {
                        hap()
                        set(k)
                    }}
                >
                    <Ico d={I[k]} />
                    {l}
                </div>
            ))}
        </nav>
    )
})

export function Sheet({ onClose, children }) {
    useEffect(() => {
        const prev = document.body.style.overflow
        document.body.style.overflow = 'hidden'
        return () => {
            document.body.style.overflow = prev
        }
    }, [])
    return createPortal(
        <>
            <div className="scrim" onClick={onClose} />
            <div className="glass sheet">{children}</div>
        </>,
        document.body
    )
}

export const Toast = memo(({ m }) => (m ? <div className="glass toast">{m}</div> : null))

const TOTAL_DEFAULT = 9e5
const C = 2 * Math.PI * 40

export const Ring = memo(function Ring({ until, total = TOTAL_DEFAULT }) {
    const target = useRef(0)
    target.current = dt(until)
    const [l, setL] = useState(() => Math.max(0, target.current - Date.now()))

    useEffect(() => {
        let id
        const tick = () => {
            const left = Math.max(0, target.current - Date.now())
            setL(prev => (Math.ceil(prev / 1000) === Math.ceil(left / 1000) && Math.abs(prev - left) < 1000 ? prev : left))
            if (left > 0) id = setTimeout(tick, 1000 - (left % 1000) + 5)
        }
        tick()
        return () => clearTimeout(id)
    }, [until])

    const s = Math.ceil(l / 1000)
    return (
        <div className="ring">
            <svg width="96" height="96" viewBox="0 0 96 96">
                <circle cx="48" cy="48" r="40" stroke="rgba(255,255,255,.1)" />
                <circle
                    cx="48"
                    cy="48"
                    r="40"
                    stroke={l < 12e4 ? '#ff6b7a' : '#9a97ff'}
                    strokeDasharray={C}
                    strokeDashoffset={C * (1 - l / total)}
                    style={{ transition: 'stroke-dashoffset 1s linear, stroke .5s' }}
                />
            </svg>
            <div className="t">
                {Math.floor(s / 60)}:{String(s % 60).padStart(2, '0')}
            </div>
        </div>
    )
})

export const Qr = memo(function Qr({ token }) {
    const ref = useRef(null)
    useEffect(() => {
        if (!ref.current) return
        const q = qrcode(0, 'M')
        q.addData(token)
        q.make()
        ref.current.innerHTML = q.createSvgTag({ cellSize: 4, margin: 0, scalable: true })
    }, [token])
    return <div className="qr" ref={ref} />
})

export const Btn = memo(function Btn({ children, onClick, ghost, busy, dis }) {
    return (
        <button
            className={'btn press' + (ghost ? ' ghost' : '')}
            disabled={busy || dis}
            onClick={e => {
                hap('medium')
                onClick && onClick(e)
            }}
        >
            {busy ? <div className="spin" /> : children}
        </button>
    )
})