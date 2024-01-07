import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Alert from 'Components/Alert';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import FilterMenu from 'Components/Menu/FilterMenu';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import TableOptionsModalWrapper from 'Components/Table/TableOptions/TableOptionsModalWrapper';
import TablePager from 'Components/Table/TablePager';
import { align, icons, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import HistoryFilterModal from './HistoryFilterModal';
import HistoryOptionsConnector from './HistoryOptionsConnector';
import HistoryRowConnector from './HistoryRowConnector';

class History extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isClearHistoryModalOpen: false
    };
  }

  //
  // Listeners

  onClearHistoryPress = () => {
    this.setState({ isClearHistoryModalOpen: true });
  };

  onClearHistoryModalClose = () => {
    this.setState({ isClearHistoryModalOpen: false });
  };

  onConfirmClearHistory = () => {
    this.setState({ isClearHistoryModalOpen: false });
    this.props.onClearHistoryPress();
  };

  //
  // Render

  render() {
    const {
      isFetching,
      isPopulated,
      isHistoryClearing,
      error,
      isIndexersFetching,
      isIndexersPopulated,
      indexersError,
      items,
      columns,
      selectedFilterKey,
      filters,
      customFilters,
      totalRecords,
      onFilterSelect,
      onFirstPagePress,
      onClearHistoryPress,
      ...otherProps
    } = this.props;

    const isFetchingAny = isFetching || isIndexersFetching;
    const isAllPopulated = isPopulated && (isIndexersPopulated || !items.length);
    const hasError = error || indexersError;

    return (
      <PageContent title={translate('History')}>
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={translate('Refresh')}
              iconName={icons.REFRESH}
              isSpinning={isFetching}
              onPress={onFirstPagePress}
            />
            <PageToolbarButton
              label={translate('Clear')}
              iconName={icons.DELETE}
              isSpinning={isHistoryClearing}
              onPress={this.onClearHistoryPress}
            />
          </PageToolbarSection>

          <PageToolbarSection alignContent={align.RIGHT}>
            <TableOptionsModalWrapper
              {...otherProps}
              columns={columns}
              optionsComponent={HistoryOptionsConnector}
            >
              <PageToolbarButton
                label={translate('Options')}
                iconName={icons.TABLE}
              />
            </TableOptionsModalWrapper>

            <FilterMenu
              alignMenu={align.RIGHT}
              selectedFilterKey={selectedFilterKey}
              filters={filters}
              customFilters={customFilters}
              filterModalConnectorComponent={HistoryFilterModal}
              onFilterSelect={onFilterSelect}
            />
          </PageToolbarSection>
        </PageToolbar>

        <PageContentBody>
          {
            isFetchingAny && !isAllPopulated &&
              <LoadingIndicator />
          }

          {
            !isFetchingAny && hasError &&
              <Alert kind={kinds.DANGER}>
                {translate('UnableToLoadHistory')}
              </Alert>
          }

          {
            // If history isPopulated and it's empty show no history found and don't
            // wait for the episodes to populate because they are never coming.

            isPopulated && !hasError && !items.length &&
              <Alert kind={kinds.INFO}>
                {translate('NoHistoryFound')}
              </Alert>
          }

          {
            isAllPopulated && !hasError && !!items.length &&
              <div>
                <Table
                  columns={columns}
                  {...otherProps}
                >
                  <TableBody>
                    {
                      items.map((item) => {
                        return (
                          <HistoryRowConnector
                            key={item.id}
                            columns={columns}
                            {...item}
                          />
                        );
                      })
                    }
                  </TableBody>
                </Table>

                <TablePager
                  totalRecords={totalRecords}
                  isFetching={isFetching}
                  onFirstPagePress={onFirstPagePress}
                  {...otherProps}
                />
              </div>
          }
        </PageContentBody>

        <ConfirmModal
          isOpen={this.state.isClearHistoryModalOpen}
          kind={kinds.DANGER}
          title={translate('ClearHistory')}
          message={translate('ClearHistoryMessageText')}
          confirmLabel={translate('Delete')}
          onConfirm={this.onConfirmClearHistory}
          onCancel={this.onClearHistoryModalClose}
        />
      </PageContent>
    );
  }
}

History.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  isHistoryClearing: PropTypes.bool.isRequired,
  error: PropTypes.object,
  isIndexersFetching: PropTypes.bool.isRequired,
  isIndexersPopulated: PropTypes.bool.isRequired,
  indexersError: PropTypes.object,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  selectedFilterKey: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
  filters: PropTypes.arrayOf(PropTypes.object).isRequired,
  customFilters: PropTypes.arrayOf(PropTypes.object).isRequired,
  totalRecords: PropTypes.number,
  onFilterSelect: PropTypes.func.isRequired,
  onFirstPagePress: PropTypes.func.isRequired,
  onClearHistoryPress: PropTypes.func.isRequired
};

export default History;
