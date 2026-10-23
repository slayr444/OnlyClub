export const TG = window.Telegram?.WebApp

if (TG) {
    TG.ready()
    TG.expand()
    TG.disableVerticalSwipes?.()
    TG.setHeaderColor?.('#07070b')
    TG.setBackgroundColor?.('#07070b')
}

export const hap = (t = 'light') => TG?.HapticFeedback?.impactOccurred(t)
export const note = t => TG?.HapticFeedback?.notificationOccurred(t)
export const tgUser = TG?.initDataUnsafe?.user