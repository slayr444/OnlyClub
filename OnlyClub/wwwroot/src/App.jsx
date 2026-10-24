import React, { useState, useCallback, useEffect, useRef, useMemo } from 'react'
import { Tabs, Toast } from './ui'
import { Home, EventScreen, Booking, Bookings, GuestList, Profile } from './screens'

const readTheme = () => {
    try {
        return localStorage.getItem('app-theme') || 'theme-1'
    } catch {
        return 'theme-1'
    }
}

export default function App() {
    const [tab, setTab] = useState('home')
    const [ev, setEv] = useState(null)
    const [bk, setBk] = useState(null)
    const [msg, setMsg] = useState('')
    const [theme, setTheme] = useState(readTheme)
    const timer = useRef(0)

    useEffect(() => {
        document.body.className = theme
        try {
            localStorage.setItem('app-theme', theme)
        } catch { }
    }, [theme])

    useEffect(() => () => clearTimeout(timer.current), [])

    const toast = useCallback(t => {
        clearTimeout(timer.current)
        setMsg(t)
        timer.current = setTimeout(() => setMsg(''), 2600)
    }, [])

    const go = useCallback(t => {
        setEv(null)
        setBk(null)
        setTab(t)
    }, [])

    const back = useCallback(() => setEv(null), [])
    const done = useCallback(() => go('tick'), [go])

    const view = useMemo(() => {
        if (bk) return <Booking b={bk} toast={toast} done={done} />
        if (ev) return <EventScreen e={ev} back={back} toast={toast} onBook={setBk} />
        if (tab === 'home') return <Home open={setEv} />
        if (tab === 'list') return <GuestList toast={toast} />
        if (tab === 'tick') return <Bookings open={setBk} toast={toast} />
        return <Profile theme={theme} setTheme={setTheme} />
    }, [bk, ev, tab, theme, toast, done, back])

    const key = bk ? 'b' + bk.id + bk.status : ev ? 'e' + ev.id : tab

    return (
        <>
            <div className="orb o1" />
            <div className="orb o2" />
            <div className="orb o3" />
            <div className="orb o4" />

            <div className="header-logo">
                <span className="logo-only">Only</span>
                <span className="logo-club">Club</span>
            </div>

            <div className="scroll" key={key}>{view}</div>
            <Tabs tab={tab} set={go} />
            <Toast m={msg} />
        </>
    )
}