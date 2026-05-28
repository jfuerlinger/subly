import { fireEvent, render, screen, within } from '@testing-library/vue'
import { describe, expect, it } from 'vitest'
import type { Subscription } from '../app/types/subscription'
import SubscriptionTable from '../components/subscriptions/SubscriptionTable.vue'

const subscriptions: Subscription[] = [
  {
    id: 'sub-1',
    name: 'Netflix',
    vendor: 'Netflix',
    logoUrl: null,
    category: 'streaming',
    price: 17.99,
    cycle: 'monthly',
    nextPaymentDate: '2026-05-20',
    paymentMethod: 'Visa',
    status: 'active',
    autoRenew: true,
    startedAt: '2023-01-01',
    cancelledAt: null,
  },
  {
    id: 'sub-2',
    name: 'Spotify',
    vendor: 'Spotify',
    logoUrl: null,
    category: 'streaming',
    price: 10.99,
    cycle: 'monthly',
    nextPaymentDate: '2026-05-21',
    paymentMethod: 'PayPal',
    status: 'paused',
    autoRenew: true,
    startedAt: '2024-02-01',
    cancelledAt: null,
  },
  {
    id: 'sub-3',
    name: 'Disney+',
    vendor: 'Disney',
    logoUrl: null,
    category: 'streaming',
    price: 12.99,
    cycle: 'monthly',
    nextPaymentDate: '2026-05-22',
    paymentMethod: 'Mastercard',
    status: 'cancelled',
    autoRenew: false,
    startedAt: '2024-03-01',
    cancelledAt: '2026-01-15',
  },
]

describe('SubscriptionTable', () => {
  it('renders status badges with localized labels and icons', () => {
    const { container } = render(SubscriptionTable, {
      props: {
        subscriptions,
      },
    })

    const netflixRow = screen.getByText('Netflix').closest('tr')
    expect(netflixRow).not.toBeNull()

    const activeBadge = netflixRow?.querySelector('.status-badge')
    expect(activeBadge).toHaveTextContent('Aktiv')
    expect(activeBadge?.querySelector('svg')).not.toBeNull()
    expect(container).not.toHaveTextContent('active')
  })

  it('filters subscriptions by multiple status values', async () => {
    render(SubscriptionTable, {
      props: {
        subscriptions,
      },
    })

    await fireEvent.click(screen.getByRole('button', { name: /alle status/i }))
    await fireEvent.click(screen.getByLabelText('Aktiv'))
    await fireEvent.click(screen.getByLabelText('Pausiert'))

    expect(screen.getByText('Netflix')).toBeInTheDocument()
    expect(screen.getByText('Spotify')).toBeInTheDocument()
    expect(screen.queryByText('Disney+')).not.toBeInTheDocument()
  })

  it('disables status action that matches current subscription status', async () => {
    const { emitted } = render(SubscriptionTable, {
      props: {
        subscriptions,
      },
    })

    const netflixRow = screen.getByText('Netflix').closest('tr')
    const spotifyRow = screen.getByText('Spotify').closest('tr')
    const disneyRow = screen.getByText('Disney+').closest('tr')

    expect(netflixRow).not.toBeNull()
    expect(spotifyRow).not.toBeNull()
    expect(disneyRow).not.toBeNull()

    const netflixButtons = within(netflixRow as HTMLElement)
    const spotifyButtons = within(spotifyRow as HTMLElement)
    const disneyButtons = within(disneyRow as HTMLElement)

    const netflixActive = netflixButtons.getByRole('button', { name: 'Aktiv' })
    const spotifyPaused = spotifyButtons.getByRole('button', { name: 'Pausiert' })
    const disneyCancelled = disneyButtons.getByRole('button', { name: 'Gekündigt' })

    expect(netflixActive).toBeDisabled()
    expect(spotifyPaused).toBeDisabled()
    expect(disneyCancelled).toBeDisabled()

    await fireEvent.click(netflixButtons.getByRole('button', { name: 'Pausiert' }))
    expect(emitted().updateStatus).toEqual([['sub-1', 'paused']])
  })

  it('emits edit event when clicking Bearbeiten', async () => {
    const { emitted } = render(SubscriptionTable, {
      props: {
        subscriptions,
      },
    })

    const netflixRow = screen.getByText('Netflix').closest('tr')
    expect(netflixRow).not.toBeNull()

    const netflixButtons = within(netflixRow as HTMLElement)
    await fireEvent.click(netflixButtons.getByRole('button', { name: 'Bearbeiten' }))

    expect(emitted().edit).toEqual([[subscriptions[0]]])
  })
})
