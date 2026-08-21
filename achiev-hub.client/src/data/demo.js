export const demoGames = [
    { id: '1', name: 'Age of Mythology', hours: 42, percentage: 67 },
    { id: '2', name: 'Hades', hours: 88, percentage: 92 },
    { id: '3', name: 'Celeste', hours: 31, percentage: 100 },
    { id: '4', name: 'Hollow Knight', hours: 54, percentage: 64 },
    { id: '5', name: 'Dead Cells', hours: 22, percentage: 18 },
    { id: '6', name: 'Stardew Valley', hours: 120, percentage: 78 },
    { id: '7', name: 'Slay the Spire', hours: 65, percentage: 55 },
    { id: '8', name: 'Ori and the Blind Forest', hours: 18, percentage: 88 },
    { id: '9', name: 'Cuphead', hours: 27, percentage: 41 },
    { id: '10', name: 'Undertale', hours: 14, percentage: 100 },
    { id: '11', name: 'Outer Wilds', hours: 36, percentage: 72 },
    { id: '12', name: 'Disco Elysium', hours: 40, percentage: 60 },
    { id: '13', name: 'Sekiro', hours: 48, percentage: 35 },
    { id: '14', name: 'Katana ZERO', hours: 8, percentage: 90 },
    { id: '15', name: 'Vampire Survivors', hours: 52, percentage: 70 },
    { id: '16', name: 'Balatro', hours: 45, percentage: 58 },
    { id: '17', name: 'Dave the Diver', hours: 33, percentage: 49 },
    { id: '18', name: 'Inscryption', hours: 19, percentage: 81 },
    { id: '19', name: 'Hades II', hours: 12, percentage: 22 },
    { id: '20', name: 'Animal Well', hours: 16, percentage: 44 }
]

export const demoAchievements = Array.from({ length: 20 }, (_, i) => ({
    id: String(i + 1),
    name: `Achievement ${i + 1}`,
    unlocked: i < 5
}))

export const demoRecentHours = [
    { name: 'Celeste', hours: '12h', percentage: '100%', status: 'Done' },
    { name: 'Hades', hours: '18h', percentage: '92%', status: 'Near' },
    { name: 'Hollow Knight', hours: '9h', percentage: '64%', status: 'Play' },
    { name: 'Dead Cells', hours: '4h', percentage: '18%', status: 'Start' },
    { name: 'Balatro', hours: '15h', percentage: '58%', status: 'Play' }
]
