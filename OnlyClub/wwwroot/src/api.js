import { TG } from './tg'
import { uid } from './utils'

const initData = TG?.initData || import.meta.env.VITE_INIT_DATA || ''

export async function call(path, opt = {}) {
    const r = await fetch('/api' + path, {
        method: opt.method || 'GET',
        headers: {
            'Content-Type': 'application/json',
            Authorization: 'tma ' + initData,
            'Idempotency-Key': uid()
        },
        body: opt.body ? JSON.stringify(opt.body) : undefined
    })

    const text = await r.text()
    let data = null
    try {
        data = text ? JSON.parse(text) : null
    } catch {
        data = text
    }

    if (!r.ok) {
        throw new Error(data?.detail || data?.title || data?.error || (typeof data === 'string' && data) || 'Ошибка ' + r.status)
    }

    return data
}

export const getEvents = () => call('/events');
export const getEventDetails = (eventId) => call(`/events/${eventId}/tables`);

export const joinGuestList = (eventId, name, age, parentEntryId = null) =>
    call(`/events/${eventId}/guestlist`, {
        method: 'POST',
        body: { name, age: Number(age), parentEntryId }
    });

export const getMyGuestList = () => call('/guestlist/my');
export const getGuestListQr = (entryId) => call(`/guestlist/${entryId}/qr`);
export const cancelGuestListEntry = (entryId) => call(`/guestlist/${entryId}/cancel`, { method: 'POST' });

export const getMyBookings = () => call('/bookings/my');

export const createBooking = (eventTableId, partySize) =>
    call('/bookings', {
        method: 'POST',
        body: { eventTableId, partySize }
    });

export const payBooking = (bookingId) =>
    call(`/bookings/${bookingId}/pay`, { method: 'POST' });

export const cancelBooking = (bookingId) =>
    call(`/bookings/${bookingId}/cancel`, { method: 'POST' });

export const getMyProfile = () => call('/me');

export const scanQrCode = (qrToken) =>
    call('/checkin/scan', {
        method: 'POST',
        body: { qr: qrToken, ageVerified: true }
    });