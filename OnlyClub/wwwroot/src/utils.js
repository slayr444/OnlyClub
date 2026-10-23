export const dt = d => new Date(/Z$|[+-]\d\d:\d\d$/.test(d) ? d : d + 'Z')

export const money = n => new Intl.NumberFormat('ru-RU').format(n) + ' ₽'

export const fmt = d =>
    dt(d).toLocaleString('ru-RU', {
        weekday: 'short',
        day: 'numeric',
        month: 'long',
        hour: '2-digit',
        minute: '2-digit'
    })

export const uid = () => crypto.randomUUID()
export const sleep = ms => new Promise(r => setTimeout(r, ms))

export const ST = {
    Free: ['Свободен', 'var(--ok)'],
    Held: ['Удержан', 'var(--warn)'],
    Confirmed: ['Занят', 'var(--bad)'],
    Seated: ['Занят', 'var(--bad)'],
    NoShow: ['Не пришли', 'var(--bad)'],
    Cancelled: ['Отменена', 'var(--mut)'],
    CheckedIn: ['Пришёл', 'var(--ok)'],
    Registered: ['Записан', 'var(--ac)']
}

export const BST = {
    Held: 'Ждёт оплаты',
    Confirmed: 'Подтверждена',
    Seated: 'За столом',
    Cancelled: 'Отменена',
    NoShow: 'Не пришли'
}

export const chipCls = s =>
    ({ Confirmed: 'ok', Seated: 'ok', CheckedIn: 'ok', Held: 'warn', Registered: 'ac', Cancelled: 'bad', NoShow: 'bad' })[s] || ''