import type { VariantProps } from 'class-variance-authority'
import { cva } from 'class-variance-authority'

export { default as Alert } from './Alert.vue'
export { default as AlertAction } from './AlertAction.vue'
export { default as AlertDescription } from './AlertDescription.vue'
export { default as AlertTitle } from './AlertTitle.vue'

export const alertVariants = cva('grid gap-0.5 rounded-lg border px-2.5 py-2 text-left text-sm has-data-[slot=alert-action]:relative has-data-[slot=alert-action]:pr-18 has-[>svg]:grid-cols-[auto_1fr] has-[>svg]:gap-x-2 *:[svg]:row-span-2 *:[svg]:translate-y-0.5 *:[svg]:text-current *:[svg:not([class*=size-])]:size-4 group/alert relative w-full', {
  variants: {
    variant: {
      default: 'bg-card text-card-foreground',
      destructive: 'text-destructive bg-card *:data-[slot=alert-description]:text-destructive/90 *:[svg]:text-current',
      purple: 'border-purple bg-purple text-purple-foreground *:data-[slot=alert-description]:text-purple-foreground/90',
      slate: 'border-slate bg-slate text-slate-foreground *:data-[slot=alert-description]:text-slate-foreground/90',
      green: 'border-green bg-green text-green-foreground *:data-[slot=alert-description]:text-green-foreground/90',
      navy: 'border-navy bg-navy text-navy-foreground *:data-[slot=alert-description]:text-navy-foreground/90',
      gray: 'border-gray bg-gray text-gray-foreground *:data-[slot=alert-description]:text-gray-foreground/90',
      ink: 'border-purple bg-ink text-ink-foreground *:data-[slot=alert-description]:text-ink-foreground/90',
      cream: 'border-cream bg-cream text-cream-foreground *:data-[slot=alert-description]:text-cream-foreground/90',
      coral: 'border-coral bg-coral text-coral-foreground *:data-[slot=alert-description]:text-coral-foreground/90',
    },
  },
  defaultVariants: {
    variant: 'default',
  },
})

export type AlertVariants = VariantProps<typeof alertVariants>
