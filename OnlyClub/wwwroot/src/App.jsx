import React, { useState, useCallback } from 'react'
import { Tabs, Toast } from './ui'
import { Home, EventScreen, Booking, Bookings, GuestList, Profile } from './screens'

export default function App() {
    const [tab, setTab] = useState('home')
    const [ev, setEv] = useState(null)
    const [bk, setBk] = useState(null)
    const [msg, setMsg] = useState('')

    const toast = useCallback(t => {
        setMsg(t)
        setTimeout(() => setMsg(''), 2600)
    }, [])

    const go = t => { setEv(null); setBk(null); setTab(t) }

    const view = bk
        ? <Booking b={bk} toast={toast} done={() => go('tick')} />
        : ev
            ? <EventScreen e={ev} back={() => setEv(null)} toast={toast} onBook={setBk} />
            : tab === 'home' ? <Home open={setEv} />
                : tab === 'list' ? <GuestList />
                    : tab === 'tick' ? <Bookings open={setBk} toast={toast} />
                        : <Profile />

    const key = bk ? 'b' + bk.id + bk.status : ev ? 'e' + ev.id : tab

    return (
        <>
            <div className="scroll" key={key}>{view}</div>
            <Tabs tab={tab} set={go} />
            <Toast m={msg} />
        </>
    )
}