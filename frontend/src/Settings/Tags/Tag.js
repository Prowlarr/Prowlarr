import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Card from 'Components/Card';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import TagDetailsModal from './Details/TagDetailsModal';
import styles from './Tag.css';

class Tag extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isDetailsModalOpen: false,
      isDeleteTagModalOpen: false
    };
  }

  //
  // Listeners

  onShowDetailsPress = () => {
    this.setState({ isDetailsModalOpen: true });
  };

  onDetailsModalClose = () => {
    this.setState({ isDetailsModalOpen: false });
  };

  onDeleteTagPress = () => {
    this.setState({
      isDetailsModalOpen: false,
      isDeleteTagModalOpen: true
    });
  };

  onDeleteTagModalClose= () => {
    this.setState({ isDeleteTagModalOpen: false });
  };

  onConfirmDeleteTag = () => {
    this.props.onConfirmDeleteTag({ id: this.props.id });
  };

  //
  // Render

  render() {
    const {
      label,
      notificationIds,
      indexerIds,
      indexerProxyIds,
      applicationIds
    } = this.props;

    const {
      isDetailsModalOpen,
      isDeleteTagModalOpen
    } = this.state;

    const isTagUsed = !!(
      indexerIds.length ||
      notificationIds.length ||
      indexerProxyIds.length ||
      applicationIds.length
    );

    return (
      <Card
        className={styles.tag}
        overlayContent={true}
        onPress={this.onShowDetailsPress}
      >
        <div className={styles.label}>
          {label}
        </div>

        {
          isTagUsed &&
            <div>
              {
                !!indexerIds.length &&
                  <div>
                    {indexerIds.length} {indexerIds.length > 1 ? translate('Indexers') : translate('Indexer')}
                  </div>
              }

              {
                !!notificationIds.length &&
                  <div>
                    {notificationIds.length} {notificationIds.length > 1 ? translate('Notifications') : translate('Notification')}
                  </div>
              }

              {
                !!indexerProxyIds.length &&
                  <div>
                    {indexerProxyIds.length} {indexerProxyIds.length > 1 ? translate('IndexerProxies') : translate('IndexerProxy')}
                  </div>
              }

              {
                !!applicationIds.length &&
                  <div>
                    {applicationIds.length} {applicationIds.length > 1 ? translate('Applications') : translate('Application')}
                  </div>
              }
            </div>
        }

        {
          !isTagUsed &&
            <div>
              {translate('NoLinks')}
            </div>
        }

        <TagDetailsModal
          label={label}
          isTagUsed={isTagUsed}
          indexerIds={indexerIds}
          notificationIds={notificationIds}
          indexerProxyIds={indexerProxyIds}
          applicationIds={applicationIds}
          isOpen={isDetailsModalOpen}
          onModalClose={this.onDetailsModalClose}
          onDeleteTagPress={this.onDeleteTagPress}
        />

        <ConfirmModal
          isOpen={isDeleteTagModalOpen}
          kind={kinds.DANGER}
          title={translate('DeleteTag')}
          message={translate('DeleteTagMessageText', { label })}
          confirmLabel={translate('Delete')}
          onConfirm={this.onConfirmDeleteTag}
          onCancel={this.onDeleteTagModalClose}
        />
      </Card>
    );
  }
}

Tag.propTypes = {
  id: PropTypes.number.isRequired,
  label: PropTypes.string.isRequired,
  notificationIds: PropTypes.arrayOf(PropTypes.number).isRequired,
  indexerIds: PropTypes.arrayOf(PropTypes.number).isRequired,
  indexerProxyIds: PropTypes.arrayOf(PropTypes.number).isRequired,
  applicationIds: PropTypes.arrayOf(PropTypes.number).isRequired,
  onConfirmDeleteTag: PropTypes.func.isRequired
};

Tag.defaultProps = {
  indexerIds: [],
  notificationIds: [],
  indexerProxyIds: [],
  applicationIds: []
};

export default Tag;
